using KNQASelfService.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace KNQASelfService.Services
{
    public class BusinessCentralSettings
    {
        public string BaseUrl { get; set; }
        public string CompanyName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public interface IBusinessCentralService
    {
        Task<List<LeaveApplication>> GetLeaveApplicationsByEmployeeAsync(string employeeNo);
        Task<LeaveApplication> GetLeaveApplicationAsync(string applicationNo);
        Task<UserSetup?> GetUserSetupAsync(string userId);
        Task<string> CreateLeaveApplicationAsync(LeaveApplicationCreate newEntry);
        Task<string> UpdateLeaveApplicationAsync(LeaveApplication application);
        Task<Employee> GetEmployeeByNoAsync(string employeeNo);
        Task<List<LeaveType>> GetLeaveTypesAsync();
        Task<decimal?> GetEmployeeLeaveBalance(string employeeNo, string leaveCode);
        Task<bool> DeleteLeaveApplicationAsync(string applicationNo, string etag);
        Task<List<Employee>> GetEmployeesAsync();
    }

    public class BusinessCentralService : IBusinessCentralService
    {
        private readonly HttpClient _httpClient;
        private readonly BusinessCentralSettings _settings;
        private readonly ILogger<BusinessCentralService> _logger;
        private readonly string _authHeaderValue;

        public BusinessCentralService(
            IConfiguration configuration,
            ILogger<BusinessCentralService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();

            _settings = new BusinessCentralSettings();
            configuration.GetSection("BusinessCentral").Bind(_settings);

            var credentials = $"{_settings.Username}:{_settings.Password}";
            _authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            _logger.LogInformation($"✅ BC Service initialized: {_settings.BaseUrl}");
        }

        private string GetFullUrl(string endpoint)
        {
            return $"{_settings.BaseUrl}/Company('{_settings.CompanyName}')/{endpoint}";
        }

        public async Task<List<LeaveApplication>> GetLeaveApplicationsByEmployeeAsync(string employeeNo)
        {
            try
            {
                var url = GetFullUrl($"Leave_Applications_List?$filter=Employee_No eq '{employeeNo}'&$orderby=Application_Date desc");

                _logger.LogInformation($"📡 GET Leave Apps for Employee: {employeeNo}");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<LeaveApplication>>(content);

                    _logger.LogInformation($"✅ Retrieved {result?.Value?.Count ?? 0} leave applications for {employeeNo}");
                    return result?.Value ?? new List<LeaveApplication>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Status: {response.StatusCode}, Error: {error}");
                    return new List<LeaveApplication>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting leave applications for employee {employeeNo}");
                return new List<LeaveApplication>();
            }
        }

        public async Task<Employee> GetEmployeeByNoAsync(string employeeNo)
        {
            try
            {
                var url = GetFullUrl($"Employees('{employeeNo}')");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var employee = JsonConvert.DeserializeObject<Employee>(content);
                    return employee;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting employee {employeeNo}");
                return null;
            }
        }

        public async Task<LeaveApplication> GetLeaveApplicationAsync(string applicationNo)
        {
            try
            {
                var url = GetFullUrl($"Leave_Applications_List?$filter=Application_No eq '{applicationNo}'");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<LeaveApplication>>(content);
                    return result?.Value?.FirstOrDefault();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting leave application {applicationNo}");
                return null;
            }
        }

        public async Task<List<LeaveType>> GetLeaveTypesAsync()
        {
            try
            {
                var url = GetFullUrl("LeaveTypes");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<LeaveType>>(content);
                    return result?.Value ?? new List<LeaveType>();
                }

                return new List<LeaveType>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting leave types");
                return new List<LeaveType>();
            }
        }

        public async Task<decimal?> GetEmployeeLeaveBalance(string employeeNo, string leaveCode)
        {
            try
            {
                // Check existing leave applications for balance
                var applicationsUrl = GetFullUrl($"Leave_Applications_List?$filter=Employee_No eq '{employeeNo}' and Leave_Code eq '{leaveCode}'&$orderby=Application_Date desc&$top=1");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.GetAsync(applicationsUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<LeaveApplication>>(content);
                    var latestApp = result?.Value?.FirstOrDefault();

                    if (latestApp != null && latestApp.LeaveBalance > 0)
                    {
                        return latestApp.LeaveBalance;
                    }
                }

                // Get default from leave type
                var leaveTypes = await GetLeaveTypesAsync();
                var leaveType = leaveTypes.FirstOrDefault(lt => lt.Code == leaveCode);

                if (leaveType != null)
                {
                    if (leaveType.UnlimitedDays)
                    {
                        return decimal.MaxValue;
                    }
                    return leaveType.Days;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting leave balance for {employeeNo}, {leaveCode}");
                return null;
            }
        }

        public async Task<string> CreateLeaveApplicationAsync(LeaveApplicationCreate newEntry)
        {
            try
            {
                _logger.LogInformation($"🎯 CREATE: Starting new leave application creation for {newEntry.EmployeeNo}");

                // Validate required fields
                if (string.IsNullOrEmpty(newEntry.EmployeeNo))
                    return "Error: Employee number is required";

                if (string.IsNullOrEmpty(newEntry.LeaveCode))
                    return "Error: Leave type is required";

                if (newEntry.DaysApplied <= 0)
                    return "Error: Days applied must be greater than 0";

                if (string.IsNullOrEmpty(newEntry.StartDate))
                    return "Error: Start date is required";

                if (string.IsNullOrEmpty(newEntry.DutiesTakenOverBy))
                    return "Error: Please select who will take over your duties";

                var url = GetFullUrl("Leave_Applications_List");

                // Parse dates
                DateTime startDate;
                if (!DateTime.TryParse(newEntry.StartDate, out startDate))
                {
                    startDate = DateTime.Today;
                }

                if (startDate < DateTime.Today)
                {
                    startDate = DateTime.Today;
                }

                var leaveTypes = await GetLeaveTypesAsync();
                var endDate = CalculateEndDate(startDate, newEntry.DaysApplied, newEntry.LeaveCode, leaveTypes);

                _logger.LogInformation($"📤 Creating leave: Type={newEntry.LeaveCode}, Days={newEntry.DaysApplied}, Start={startDate:yyyy-MM-dd}, End={endDate:yyyy-MM-dd}");

                // Minimal payload without fields that might cause assembly errors
                var payload = new
                {
                    Employee_No = newEntry.EmployeeNo,
                    Leave_Code = newEntry.LeaveCode,
                    Days_Applied = newEntry.DaysApplied,
                    Start_Date = startDate.ToString("yyyy-MM-dd"),
                    End_Date = endDate.ToString("yyyy-MM-dd"),
                    Duties_Taken_Over_By = newEntry.DutiesTakenOverBy,
                    Application_Date = DateTime.Today.ToString("yyyy-MM-dd")
                    // NOTE: Removed Reason, Telephone_No, Alternate_Phone_No to avoid BC assembly error
                    // These can be added via UPDATE after creation if needed
                };

                var jsonContent = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                _logger.LogInformation($"📤 Payload: {jsonContent}");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"✅ Leave application created successfully!");

                    // Try to extract the created application number
                    try
                    {
                        var createdApp = JsonConvert.DeserializeObject<LeaveApplication>(responseContent);

                        // If we have Reason, Telephone, or Alternate Phone, update them now
                        if (!string.IsNullOrEmpty(newEntry.Reason) ||
                            !string.IsNullOrEmpty(newEntry.TelephoneNo) ||
                            !string.IsNullOrEmpty(newEntry.AlternatePhoneNo))
                        {
                            createdApp.Reason = newEntry.Reason;
                            createdApp.TelephoneNo = newEntry.TelephoneNo;
                            createdApp.AlternatePhoneNo = newEntry.AlternatePhoneNo;

                            var updateResult = await UpdateLeaveApplicationAsync(createdApp);
                            _logger.LogInformation($"📝 Additional fields update: {updateResult}");
                        }
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogWarning($"⚠️ Could not update additional fields: {updateEx.Message}");
                    }

                    return "Success: Leave application created successfully";
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Status Code: {response.StatusCode}");
                    _logger.LogError($"❌ Response: {errorResponse}");

                    return ParseBusinessCentralError(errorResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception creating leave application");
                return $"Error: {ex.Message}";
            }
        }

        private DateTime CalculateEndDate(DateTime startDate, int days, string leaveCode, List<LeaveType> leaveTypes)
        {
            var leaveType = leaveTypes.FirstOrDefault(lt => lt.Code == leaveCode);

            DateTime endDate = startDate;
            int daysToAdd = days - 1;

            while (daysToAdd > 0)
            {
                endDate = endDate.AddDays(1);
                bool skipDay = false;

                if (endDate.DayOfWeek == DayOfWeek.Sunday && leaveType?.Inclusive_of_Sunday != true)
                    skipDay = true;

                if (endDate.DayOfWeek == DayOfWeek.Saturday && leaveType?.Inclusive_of_Saturday != true)
                    skipDay = true;

                if (!skipDay)
                    daysToAdd--;
            }

            return endDate;
        }

        private string ParseBusinessCentralError(string errorResponse)
        {
            try
            {
                if (string.IsNullOrEmpty(errorResponse))
                    return "Unknown error from Business Central";

                if (errorResponse.Contains("System.Diagnostics.Debug") && errorResponse.Contains("Could not load file or assembly"))
                {
                    return "Error: Business Central configuration issue. The application has been created but some fields may not be saved. Please contact your administrator.";
                }

                if (errorResponse.Trim().StartsWith("{"))
                {
                    using (var doc = System.Text.Json.JsonDocument.Parse(errorResponse))
                    {
                        if (doc.RootElement.TryGetProperty("error", out var errorElement))
                        {
                            if (errorElement.TryGetProperty("message", out var messageElement))
                            {
                                var message = messageElement.GetString();
                                if (!string.IsNullOrEmpty(message))
                                {
                                    var correlationIndex = message.IndexOf("CorrelationId:");
                                    if (correlationIndex > 0)
                                    {
                                        return message.Substring(0, correlationIndex).Trim();
                                    }
                                    return message;
                                }
                            }
                        }
                    }
                }

                return errorResponse.Length > 500 ? errorResponse.Substring(0, 500) + "..." : errorResponse;
            }
            catch
            {
                return errorResponse.Length > 200 ? errorResponse.Substring(0, 200) + "..." : errorResponse;
            }
        }

        public async Task<string> UpdateLeaveApplicationAsync(LeaveApplication application)
        {
            try
            {
                _logger.LogInformation($"🔄 UPDATE: Updating leave application {application.ApplicationNo}");

                if (string.IsNullOrEmpty(application.ApplicationNo))
                    return "Error: Application number is required";

                // FIRST: Fetch the current record to get the latest ETag
                var currentRecord = await GetLeaveApplicationAsync(application.ApplicationNo);
                if (currentRecord == null)
                    return "Error: Application not found or may have been deleted";

                _logger.LogInformation($"📡 Current ETag: {currentRecord.ODataEtag}");

                var url = GetFullUrl($"Leave_Applications_List('{application.ApplicationNo}')");

                // Create a new HttpClient instance for this request
                // This avoids header contamination from previous requests
                using var requestClient = new HttpClient();

                requestClient.DefaultRequestHeaders.Clear();
                requestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
                requestClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // CRITICAL: Properly format the ETag for Business Central
                if (!string.IsNullOrEmpty(currentRecord.ODataEtag))
                {
                    try
                    {
                        // Business Central ETags often come as: W/"JzIwOzEzNjEzMTY5NDc1Mzg5Mjc1Nzc3MTswMDsn"
                        // The format needs to be: "W/\"datetime'2024-01-01T00:00:00.000Z'\""
                        // But actually, we should use it exactly as received or wrap it properly

                        var etagValue = currentRecord.ODataEtag;

                        // If it starts with W/ but doesn't have quotes, add them
                        if (etagValue.StartsWith("W/") && !etagValue.StartsWith("W/\""))
                        {
                            etagValue = $"W/\"{etagValue.Substring(2)}\"";
                        }
                        // If it doesn't start with W/, add it
                        else if (!etagValue.StartsWith("W/"))
                        {
                            etagValue = $"W/\"{etagValue}\"";
                        }
                        // Ensure it has the full format
                        else if (!etagValue.Contains("\""))
                        {
                            etagValue = etagValue.Replace("W/", "W/\"") + "\"";
                        }

                        _logger.LogInformation($"📡 Formatted ETag: {etagValue}");
                        requestClient.DefaultRequestHeaders.Add("If-Match", etagValue);
                    }
                    catch (Exception etagEx)
                    {
                        _logger.LogWarning($"⚠️ Error formatting ETag: {etagEx.Message}");
                        _logger.LogWarning($"⚠️ Raw ETag: {currentRecord.ODataEtag}");

                        // Try a simpler approach
                        var simpleEtag = currentRecord.ODataEtag.Trim();
                        requestClient.DefaultRequestHeaders.Add("If-Match", simpleEtag);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ No ETag found, trying without If-Match header");
                }

                // Parse and validate dates
                DateTime startDate;
                if (!DateTime.TryParse(application.StartDate, out startDate))
                {
                    return "Error: Invalid start date format";
                }

                // Calculate end date
                var leaveTypes = await GetLeaveTypesAsync();
                var endDate = CalculateEndDate(startDate, application.DaysApplied, application.LeaveCode, leaveTypes);

                // IMPORTANT: Only include fields that are actually updatable in Business Central
                var updatePayload = new Dictionary<string, object>
                {
                    ["Leave_Code"] = application.LeaveCode,
                    ["Days_Applied"] = application.DaysApplied,
                    ["Start_Date"] = startDate.ToString("yyyy-MM-dd"),
                    ["End_Date"] = endDate.ToString("yyyy-MM-dd"),
                    ["Duties_Taken_Over_By"] = application.DutiesTakenOverBy
                };

                // Only include optional fields if they have values
                if (!string.IsNullOrEmpty(application.Reason))
                    updatePayload["Reason"] = application.Reason;

                if (!string.IsNullOrEmpty(application.TelephoneNo))
                    updatePayload["Telephone_No"] = application.TelephoneNo;

                if (!string.IsNullOrEmpty(application.AlternatePhoneNo))
                    updatePayload["Alternate_Phone_No"] = application.AlternatePhoneNo;

                var jsonContent = JsonConvert.SerializeObject(updatePayload, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-dd"
                });

                _logger.LogInformation($"📤 Update Payload: {jsonContent}");

                // Use PATCH method for update
                var request = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                var response = await requestClient.SendAsync(request);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation($"✅ Leave application {application.ApplicationNo} updated successfully");
                    return "Success: Leave application updated successfully";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                {
                    // ETag mismatch - record was modified by someone else
                    _logger.LogError($"❌ ETag mismatch - record modified by another user");
                    return "Error: This record was modified by another user. Please refresh and try again.";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError($"❌ Application not found: {application.ApplicationNo}");
                    return "Error: Application not found or may have been deleted";
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Update failed. Status: {response.StatusCode}");
                    _logger.LogError($"❌ Error: {errorResponse}");

                    // Check for specific ETag format error
                    if (errorResponse.Contains("format of value") && errorResponse.Contains("is invalid"))
                    {
                        return "Error: Concurrency token issue. Please contact administrator.";
                    }

                    return ParseBusinessCentralError(errorResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating leave application");
                return $"Error: {ex.Message}";
            }
        }
        public async Task<bool> DeleteLeaveApplicationAsync(string applicationNo, string etag)
        {
            try
            {
                var url = GetFullUrl($"Leave_Applications_List('{applicationNo}')");

                _logger.LogInformation($"🗑️ DELETE: {url}");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (!string.IsNullOrEmpty(etag))
                {
                    _httpClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation($"✅ Leave application {applicationNo} deleted successfully");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting leave application {applicationNo}");
                return false;
            }
        }

        public async Task<UserSetup?> GetUserSetupAsync(string userId)
        {
            try
            {
                var url = GetFullUrl($"UserSetup?$filter=User_ID eq '{userId}'");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<UserSetup>>(content);
                    return result?.Value?.FirstOrDefault();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Exception getting UserSetup for {userId}");
                throw;
            }
        }

        public async Task<List<Employee>> GetEmployeesAsync()
        {
            try
            {
                var url = GetFullUrl("Employees?$filter=Status eq 'Active'&$select=No,FullName,Status");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<Employee>>(content);
                    return result?.Value?.Where(e => !string.IsNullOrEmpty(e.No)).ToList() ?? new List<Employee>();
                }

                return new List<Employee>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting employees");
                return new List<Employee>();
            }
        }
    }

    public class ODataResponse<T>
    {
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; }

        [JsonProperty("value")]
        public List<T> Value { get; set; }
    }
}
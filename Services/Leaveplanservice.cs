using KNQASelfService.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace KNQASelfService.Services
{
    /// <summary>
    /// Leave Plan Service
    /// Handles all CRUD operations for Leave Plans via Business Central API
    /// </summary>
    public class LeavePlanService : ILeavePlanService
    {
        private readonly HttpClient _httpClient;
        private readonly BusinessCentralSettings _settings;
        private readonly ILogger<LeavePlanService> _logger;
        private readonly string _authHeaderValue;

        public LeavePlanService(
            IConfiguration configuration,
            ILogger<LeavePlanService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();

            _settings = new BusinessCentralSettings();
            configuration.GetSection("BusinessCentral").Bind(_settings);

            var credentials = $"{_settings.Username}:{_settings.Password}";
            _authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            _logger.LogInformation($"✅ Leave Plan Service initialized: {_settings.BaseUrl}");
        }

        #region Helper Methods

        /// <summary>
        /// Constructs the full API URL for a given endpoint
        /// </summary>
        private string GetFullUrl(string endpoint)
        {
            return $"{_settings.BaseUrl}/Company('{_settings.CompanyName}')/{endpoint}";
        }

        /// <summary>
        /// Sets up standard HTTP headers for API requests
        /// </summary>
        private void SetupHttpHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Parses error responses from Business Central
        /// </summary>
        private string ParseBusinessCentralError(string errorResponse)
        {
            try
            {
                if (string.IsNullOrEmpty(errorResponse))
                    return "Unknown error from Business Central";

                // Try parsing as JSON error response
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
                                    // Remove correlation ID if present
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
            catch (Exception ex)
            {
                _logger.LogWarning($"Error parsing BC error: {ex.Message}");
                return errorResponse.Length > 200 ? errorResponse.Substring(0, 200) + "..." : errorResponse;
            }
        }

        #endregion

        #region READ Operations

        /// <summary>
        /// Get all Leave Plans for a specific employee (from Leave_Plan_List)
        /// </summary>
        public async Task<List<LeavePlan>> GetLeavePlansByEmployeeAsync(string employeeNo)
        {
            try
            {
                var url = GetFullUrl($"Leave_Plan_List?$filter=Employee_No eq '{employeeNo}'&$orderby=Application_Date desc");

                _logger.LogInformation($"📡 GET Leave Plans for Employee: {employeeNo}");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<LeavePlanODataResponse>(content);

                    _logger.LogInformation($"✅ Retrieved {result?.Value?.Count ?? 0} leave plans for {employeeNo}");
                    return result?.Value ?? new List<LeavePlan>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Status: {response.StatusCode}, Error: {error}");
                    return new List<LeavePlan>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting leave plans for employee {employeeNo}");
                return new List<LeavePlan>();
            }
        }

        /// <summary>
        /// Get a single Leave Plan by Application Number (from Leave_Plan_List)
        /// </summary>
        public async Task<LeavePlan> GetLeavePlanAsync(string applicationNo)
        {
            try
            {
                var url = GetFullUrl($"Leave_Plan_List?$filter=Application_No eq '{applicationNo}'");

                _logger.LogInformation($"📡 GET Leave Plan: {applicationNo}");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<LeavePlanODataResponse>(content);
                    var plan = result?.Value?.FirstOrDefault();

                    if (plan != null)
                    {
                        _logger.LogInformation($"✅ Retrieved leave plan {applicationNo}");
                    }

                    return plan;
                }

                _logger.LogWarning($"⚠️ Leave plan {applicationNo} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting leave plan {applicationNo}");
                return null;
            }
        }

        /// <summary>
        /// Get Leave Plan Application by Application Number (from Leave_Plan_Application)
        /// </summary>
        private async Task<LeavePlanApplication> GetLeavePlanApplicationAsync(string applicationNo)
        {
            try
            {
                // Use filter to get from Leave_Plan_Application
                var url = GetFullUrl($"Leave_Plan_Application?$filter=Application_No eq '{applicationNo}'");

                _logger.LogInformation($"📡 GET Leave Plan Application: {applicationNo} via filter");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<LeavePlanApplicationODataResponse>(content);
                    var application = result?.Value?.FirstOrDefault();

                    if (application != null)
                    {
                        _logger.LogInformation($"✅ Retrieved leave plan application {applicationNo}");
                        return application;
                    }
                }

                _logger.LogWarning($"⚠️ Leave plan application {applicationNo} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting leave plan application {applicationNo}");
                return null;
            }
        }

        /// <summary>
        /// Get Leave Plans with advanced filtering (from Leave_Plan_List)
        /// </summary>
        public async Task<List<LeavePlan>> GetLeavePlansAsync(LeavePlanFilter filter)
        {
            try
            {
                var filterParts = new List<string>();

                if (!string.IsNullOrEmpty(filter.EmployeeNo))
                    filterParts.Add($"Employee_No eq '{filter.EmployeeNo}'");

                if (!string.IsNullOrEmpty(filter.Status))
                    filterParts.Add($"Status eq '{filter.Status}'");

                if (!string.IsNullOrEmpty(filter.LeaveCode))
                    filterParts.Add($"Leave_Code eq '{filter.LeaveCode}'");

                var filterQuery = filterParts.Count > 0 ? $"&$filter={string.Join(" and ", filterParts)}" : "";
                var orderBy = $"&$orderby={Uri.EscapeDataString(filter.OrderBy ?? "Application_Date desc")}";

                var url = GetFullUrl($"Leave_Plan_List?{filterQuery}{orderBy}");

                _logger.LogInformation($"📡 GET Leave Plans with filters");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<LeavePlanODataResponse>(content);
                    return result?.Value ?? new List<LeavePlan>();
                }

                return new List<LeavePlan>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting leave plans with filters");
                return new List<LeavePlan>();
            }
        }

        /// <summary>
        /// Get Leave Plans by status (from Leave_Plan_List)
        /// </summary>
        public async Task<List<LeavePlan>> GetLeavePlansByStatusAsync(string employeeNo, string status)
        {
            var filter = new LeavePlanFilter
            {
                EmployeeNo = employeeNo,
                Status = status
            };
            return await GetLeavePlansAsync(filter);
        }

        /// <summary>
        /// Get Leave Plan summary statistics (from Leave_Plan_List)
        /// </summary>
        public async Task<LeavePlanSummary> GetLeavePlanSummaryAsync(string employeeNo)
        {
            try
            {
                var plans = await GetLeavePlansByEmployeeAsync(employeeNo);

                var summary = new LeavePlanSummary
                {
                    TotalPlans = plans.Count,
                    ActivePlans = plans.Count(p => p.Status == "Active"),
                    InactiveePlans = plans.Count(p => p.Status != "Active"),
                    TotalEntitlement = plans.Sum(p => p.LeaveEntitlement),
                    TotalBalance = plans.Sum(p => p.LeaveBalance)
                };

                _logger.LogInformation($"✅ Generated summary for {employeeNo}: {summary.TotalPlans} plans");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error generating summary for {employeeNo}");
                return new LeavePlanSummary();
            }
        }

        #endregion

        #region CREATE Operations

        /// <summary>
        /// Create a new Leave Plan Application
        /// </summary>
        public async Task<(bool Success, string Message, LeavePlan Data)> CreateLeavePlanAsync(LeavePlanCreate leavePlan)
        {
            try
            {
                _logger.LogInformation($"🎯 CREATE: Starting new leave plan creation for {leavePlan.EmployeeNo}");

                // Validate required fields
                var validationErrors = await ValidateLeavePlanAsync(leavePlan);
                if (validationErrors.Any())
                {
                    var errorMessage = string.Join("; ", validationErrors);
                    _logger.LogWarning($"❌ Validation failed: {errorMessage}");
                    return (false, errorMessage, null);
                }

                var url = GetFullUrl("Leave_Plan_Application");

                // Parse and validate dates
                if (!DateTime.TryParse(leavePlan.FiscalStartDate, out var fiscalStart))
                {
                    return (false, "Error: Invalid Fiscal Start Date format", null);
                }

                if (!DateTime.TryParse(leavePlan.MaturityDate, out var maturity))
                {
                    return (false, "Error: Invalid Maturity Date format", null);
                }

                _logger.LogInformation($"📤 Creating leave plan: EmployeeNo={leavePlan.EmployeeNo}, LeaveCode={leavePlan.LeaveCode}, Entitlement={leavePlan.LeaveEntitlement}");

                // Based on the error "primary key must be integer", we need to handle this carefully
                // The API returns string Application_No (like "LVP0007") but may expect integer on create

                // APPROACH 1: Minimal payload - let BC generate everything
                object payload = new
                {
                    Employee_No = leavePlan.EmployeeNo,
                    Leave_Code = leavePlan.LeaveCode,
                    Days_in_Plan = leavePlan.DaysInPlan,
                    Fiscal_Start_Date = fiscalStart.ToString("yyyy-MM-dd"),
                    Maturity_Date = maturity.ToString("yyyy-MM-dd"),
                    Leave_Entitlement = leavePlan.LeaveEntitlement
                    // DO NOT include: Application_No, Application_Date, User_ID
                };

                var jsonContent = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-dd"
                });

                _logger.LogInformation($"📤 Payload: {jsonContent}");

                SetupHttpHeaders();
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"✅ Leave plan created successfully!");

                    try
                    {
                        var createdApplication = JsonConvert.DeserializeObject<LeavePlanApplication>(responseContent);

                        // Convert to LeavePlan for return
                        var createdPlan = new LeavePlan
                        {
                            ApplicationNo = createdApplication.ApplicationNo,
                            EmployeeNo = createdApplication.EmployeeNo,
                            EmployeeName = createdApplication.EmployeeName,
                            LeaveCode = createdApplication.LeaveCode,
                            DaysInPlan = createdApplication.DaysInPlan,
                            FiscalStartDate = createdApplication.FiscalStartDate,
                            MaturityDate = createdApplication.MaturityDate,
                            LeaveEntitlement = createdApplication.LeaveEntitlement,
                            LeaveBalance = createdApplication.LeaveBalance,
                            LeaveEarnedToDate = createdApplication.LeaveEarnedToDate,
                            ApplicationDate = createdApplication.ApplicationDate,
                            UserId = createdApplication.UserId,
                            DepartmentCode = createdApplication.DepartmentCode,
                            Designation = createdApplication.Designation,
                            DateOfJoiningCompany = createdApplication.DateOfJoiningCompany,
                            OffDays = createdApplication.OffDays,
                            Status = "Active"
                        };

                        return (true, "Leave plan created successfully", createdPlan);
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning($"⚠️ Could not parse created plan: {parseEx.Message}");
                        return (true, "Leave plan created successfully (details not retrieved)", null);
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Status Code: {response.StatusCode}");
                    _logger.LogError($"❌ Response: {errorResponse}");

                    // Check for assembly error - this is a server-side issue
                    if (errorResponse.Contains("System.Diagnostics.Debug"))
                    {
                        _logger.LogError($"❌ CRITICAL SERVER ERROR: Missing .NET assembly");
                        return (false, "SERVER ERROR: Business Central is missing required .NET dependencies. Please contact your system administrator to install 'System.Diagnostics.Debug' version 4.1.2.0.", null);
                    }

                    // If primary key error, try alternative approach
                    if (errorResponse.Contains("primary key") || errorResponse.Contains("Integer"))
                    {
                        _logger.LogInformation($"🔄 Primary key error detected. Trying alternative approach...");

                        // APPROACH 2: Try using Journal API if available
                        try
                        {
                            var journalUrl = GetFullUrl("LeavePlanJournal");
                            var journalResponse = await _httpClient.PostAsync(journalUrl, content);

                            if (journalResponse.IsSuccessStatusCode)
                            {
                                _logger.LogInformation($"✅ Created via Journal API");
                                return (true, "Leave plan created via Journal API", null);
                            }
                        }
                        catch (Exception journalEx)
                        {
                            _logger.LogDebug($"Journal API failed: {journalEx.Message}");
                        }

                        return (false, "Cannot create leave plan. The system requires specific fields that are auto-generated.", null);
                    }

                    var errorMessage = ParseBusinessCentralError(errorResponse);
                    return (false, errorMessage, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception creating leave plan");
                return (false, $"Error: {ex.Message}", null);
            }
        }

        #endregion

        #region UPDATE Operations

        /// <summary>
        /// Update an existing Leave Plan Application
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateLeavePlanAsync(LeavePlanUpdate leavePlanUpdate, string etag)
        {
            try
            {
                _logger.LogInformation($"🔄 UPDATE: Updating leave plan {leavePlanUpdate.ApplicationNo}");

                if (string.IsNullOrEmpty(leavePlanUpdate.ApplicationNo))
                    return (false, "Error: Application number is required");

                // Get current application to verify it exists and get ETag
                var currentApplication = await GetLeavePlanApplicationAsync(leavePlanUpdate.ApplicationNo);
                if (currentApplication == null)
                {
                    _logger.LogError($"❌ Leave plan application {leavePlanUpdate.ApplicationNo} not found");
                    return (false, "Error: Leave plan not found or may have been deleted");
                }

                string currentEtag = !string.IsNullOrEmpty(etag) ? etag : currentApplication.ODataEtag;
                _logger.LogInformation($"📡 Current ETag: {currentEtag}");

                // Use Application_No as the key in the URL
                var url = GetFullUrl($"Leave_Plan_Application('{leavePlanUpdate.ApplicationNo}')");
                _logger.LogInformation($"📡 Update URL: {url}");

                // Create new HttpClient for this request
                using var requestClient = new HttpClient();
                requestClient.DefaultRequestHeaders.Clear();
                requestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
                requestClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add ETag for concurrency control
                if (!string.IsNullOrEmpty(currentEtag))
                {
                    requestClient.DefaultRequestHeaders.Add("If-Match", currentEtag);
                }

                // Prepare update payload - only include fields we want to change
                var updatePayload = new Dictionary<string, object>();

                // Include all fields that should be updated
                updatePayload["Leave_Code"] = leavePlanUpdate.LeaveCode;
                updatePayload["Days_in_Plan"] = leavePlanUpdate.DaysInPlan;
                updatePayload["Leave_Entitlement"] = leavePlanUpdate.LeaveEntitlement;

                // Add date fields if provided
                if (!string.IsNullOrEmpty(leavePlanUpdate.MaturityDate))
                    updatePayload["Maturity_Date"] = leavePlanUpdate.MaturityDate;

                if (!string.IsNullOrEmpty(leavePlanUpdate.FiscalStartDate))
                    updatePayload["Fiscal_Start_Date"] = leavePlanUpdate.FiscalStartDate;

                var jsonContent = JsonConvert.SerializeObject(updatePayload, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-dd"
                });

                _logger.LogInformation($"📤 Update Payload: {jsonContent}");

                // Use PATCH for update
                var request = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                var response = await requestClient.SendAsync(request);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation($"✅ Leave plan {leavePlanUpdate.ApplicationNo} updated successfully");
                    return (true, "Leave plan updated successfully");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                {
                    _logger.LogError($"❌ ETag mismatch - record modified by another user");
                    return (false, "Error: This record was modified by another user. Please refresh and try again.");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError($"❌ Leave plan not found: {leavePlanUpdate.ApplicationNo}");

                    // Try alternative: Maybe the key is numeric?
                    if (int.TryParse(leavePlanUpdate.ApplicationNo.Replace("LVP", ""), out int numericId))
                    {
                        _logger.LogInformation($"🔄 Trying with numeric ID: {numericId}");
                        var numericUrl = GetFullUrl($"Leave_Plan_Application({numericId})");
                        request.RequestUri = new Uri(numericUrl);

                        response = await requestClient.SendAsync(request);

                        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                        {
                            _logger.LogInformation($"✅ Updated with numeric ID");
                            return (true, "Leave plan updated successfully");
                        }
                    }

                    return (false, "Error: Leave plan not found or may have been deleted");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Update failed. Status: {response.StatusCode}");
                    _logger.LogError($"❌ Error: {errorResponse}");

                    var errorMessage = ParseBusinessCentralError(errorResponse);
                    return (false, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating leave plan");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Update Leave Plan status
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateLeavePlanStatusAsync(string applicationNo, string newStatus, string etag)
        {
            try
            {
                var plan = await GetLeavePlanAsync(applicationNo);
                if (plan == null)
                    return (false, "Error: Leave plan not found");

                var updateModel = new LeavePlanUpdate
                {
                    ApplicationNo = applicationNo,
                    LeaveCode = plan.LeaveCode,
                    DaysInPlan = plan.DaysInPlan,
                    LeaveEntitlement = plan.LeaveEntitlement,
                    FiscalStartDate = plan.FiscalStartDate,
                    MaturityDate = plan.MaturityDate
                };

                return await UpdateLeavePlanAsync(updateModel, etag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating status for {applicationNo}");
                return (false, $"Error: {ex.Message}");
            }
        }

        #endregion

        #region DELETE Operations

        /// <summary>
        /// Delete a Leave Plan
        /// </summary>
        public async Task<bool> DeleteLeavePlanAsync(string applicationNo, string etag)
        {
            try
            {
                var url = GetFullUrl($"Leave_Plan_Application('{applicationNo}')");

                _logger.LogInformation($"🗑️ DELETE: {applicationNo}");

                SetupHttpHeaders();

                if (!string.IsNullOrEmpty(etag))
                {
                    _httpClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation($"✅ Leave plan {applicationNo} deleted successfully");
                    return true;
                }

                _logger.LogError($"❌ Delete failed with status {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting leave plan {applicationNo}");
                return false;
            }
        }

        #endregion

        #region VALIDATION Operations

        /// <summary>
        /// Validate Leave Plan data
        /// </summary>
        public async Task<List<string>> ValidateLeavePlanAsync(LeavePlanCreate leavePlan)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(leavePlan.EmployeeNo))
                errors.Add("Employee number is required");

            if (string.IsNullOrEmpty(leavePlan.LeaveCode))
                errors.Add("Leave type is required");

            if (leavePlan.DaysInPlan <= 0)
                errors.Add("Days in plan must be greater than 0");

            if (leavePlan.LeaveEntitlement < 0)
                errors.Add("Leave entitlement cannot be negative");

            if (string.IsNullOrEmpty(leavePlan.FiscalStartDate))
                errors.Add("Fiscal start date is required");

            if (!DateTime.TryParse(leavePlan.FiscalStartDate, out var fiscalStart))
                errors.Add("Invalid Fiscal start date format");

            if (!DateTime.TryParse(leavePlan.MaturityDate, out var maturity))
                errors.Add("Invalid Maturity date format");

            if (DateTime.TryParse(leavePlan.FiscalStartDate, out var start) &&
                DateTime.TryParse(leavePlan.MaturityDate, out var mat) &&
                mat <= start)
                errors.Add("Maturity date must be after fiscal start date");

            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Check if Leave Plan exists
        /// </summary>
        public async Task<bool> LeavePlanExistsAsync(string applicationNo)
        {
            try
            {
                var plan = await GetLeavePlanAsync(applicationNo);
                return plan != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if API endpoints are working
        /// </summary>
        public async Task<(bool ListApiWorking, bool ApplicationApiWorking)> CheckApisAsync()
        {
            try
            {
                var listUrl = GetFullUrl("Leave_Plan_List?$top=1");
                var appUrl = GetFullUrl("Leave_Plan_Application?$top=1");

                SetupHttpHeaders();

                var listResponse = await _httpClient.GetAsync(listUrl);
                var appResponse = await _httpClient.GetAsync(appUrl);

                return (listResponse.IsSuccessStatusCode, appResponse.IsSuccessStatusCode);
            }
            catch
            {
                return (false, false);
            }
        }

        #endregion
    }
}
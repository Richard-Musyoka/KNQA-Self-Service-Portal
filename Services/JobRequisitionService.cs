// JobRequisitionService.cs
using KNQASelfService.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace KNQASelfService.Services
{
    public class JobRequisitionService : IJobRequisitionService
    {
        private readonly HttpClient _httpClient;
        private readonly BusinessCentralSettings _settings;
        private readonly ILogger<JobRequisitionService> _logger;
        private readonly string _authHeaderValue;

        public JobRequisitionService(
            IConfiguration configuration,
            ILogger<JobRequisitionService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();

            _settings = new BusinessCentralSettings();
            configuration.GetSection("BusinessCentral").Bind(_settings);

            var credentials = $"{_settings.Username}:{_settings.Password}";
            _authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            _logger.LogInformation($"✅ Job Requisition Service initialized: {_settings.BaseUrl}");
        }

        #region Helper Methods
        private string GetFullUrl(string endpoint)
        {
            return $"{_settings.BaseUrl}/Company('{_settings.CompanyName}')/{endpoint}";
        }

        private void SetupHttpHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private string ParseBusinessCentralError(string errorResponse)
        {
            try
            {
                if (string.IsNullOrEmpty(errorResponse))
                    return "Unknown error from Business Central";

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
            catch (Exception ex)
            {
                _logger.LogWarning($"Error parsing BC error: {ex.Message}");
                return errorResponse.Length > 200 ? errorResponse.Substring(0, 200) + "..." : errorResponse;
            }
        }
        #endregion

        #region READ Operations
        public async Task<List<JobRequisition>> GetJobRequisitionsByEmployeeAsync(string employeeNo)
        {
            try
            {
                var url = GetFullUrl($"Job_Requisition_List?$filter=Employee_No eq '{employeeNo}'&$orderby=Document_Date desc");

                _logger.LogInformation($"📡 GET Job Requisitions for Employee: {employeeNo}");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<JobRequisitionODataResponse>(content);

                    _logger.LogInformation($"✅ Retrieved {result?.Value?.Count ?? 0} job requisitions for {employeeNo}");
                    return result?.Value ?? new List<JobRequisition>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Status: {response.StatusCode}, Error: {error}");
                    return new List<JobRequisition>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting job requisitions for employee {employeeNo}");
                return new List<JobRequisition>();
            }
        }

        public async Task<JobRequisition> GetJobRequisitionAsync(string applicationNo)
        {
            try
            {
                var url = GetFullUrl($"Job_Requisition_List?$filter=Application_No eq '{applicationNo}'");

                _logger.LogInformation($"📡 GET Job Requisition: {applicationNo}");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<JobRequisitionODataResponse>(content);
                    var requisition = result?.Value?.FirstOrDefault();

                    if (requisition != null)
                    {
                        _logger.LogInformation($"✅ Retrieved job requisition {applicationNo}");
                    }

                    return requisition;
                }

                _logger.LogWarning($"⚠️ Job requisition {applicationNo} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting job requisition {applicationNo}");
                return null;
            }
        }

        public async Task<List<JobRequisition>> GetJobRequisitionsAsync(JobRequisitionFilter filter)
        {
            try
            {
                var filterParts = new List<string>();

                if (!string.IsNullOrEmpty(filter.DepartmentCode))
                    filterParts.Add($"Department_Code eq '{filter.DepartmentCode}'");

                if (!string.IsNullOrEmpty(filter.Status))
                    filterParts.Add($"Status eq '{filter.Status}'");

                if (!string.IsNullOrEmpty(filter.EmploymentType))
                    filterParts.Add($"Employment_Type eq '{filter.EmploymentType}'");

                if (filter.FromDate.HasValue)
                    filterParts.Add($"Document_Date ge {filter.FromDate.Value:yyyy-MM-dd}");

                if (filter.ToDate.HasValue)
                    filterParts.Add($"Document_Date le {filter.ToDate.Value:yyyy-MM-dd}");

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    filterParts.Add($"(contains(Job_Position, '{filter.SearchTerm}') or contains(Application_No, '{filter.SearchTerm}'))");
                }

                var filterQuery = filterParts.Count > 0 ? $"&$filter={string.Join(" and ", filterParts)}" : "";
                var orderBy = $"&$orderby={Uri.EscapeDataString(filter.OrderBy)}";

                var url = GetFullUrl($"Job_Requisition_List?{filterQuery}{orderBy}");

                _logger.LogInformation($"📡 GET Job Requisitions with filters");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<JobRequisitionODataResponse>(content);
                    return result?.Value ?? new List<JobRequisition>();
                }

                return new List<JobRequisition>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting job requisitions with filters");
                return new List<JobRequisition>();
            }
        }

        public async Task<List<JobRequisition>> GetJobRequisitionsByStatusAsync(string status)
        {
            var filter = new JobRequisitionFilter
            {
                Status = status
            };
            return await GetJobRequisitionsAsync(filter);
        }

        public async Task<JobRequisitionSummary> GetJobRequisitionSummaryAsync()
        {
            try
            {
                var allRequisitions = await GetJobRequisitionsAsync(new JobRequisitionFilter());

                var summary = new JobRequisitionSummary
                {
                    TotalRequisitions = allRequisitions.Count,
                    PendingRequisitions = allRequisitions.Count(r => r.Status == "Pending"),
                    ApprovedRequisitions = allRequisitions.Count(r => r.Status == "Approved"),
                    RejectedRequisitions = allRequisitions.Count(r => r.Status == "Rejected"),
                    OpenPositions = allRequisitions.Where(r => r.Status == "Approved").Sum(r => r.Positions),
                    FilledPositions = 0, // This would need separate tracking
                    TotalBudget = allRequisitions.Where(r => r.Status == "Approved").Sum(r => r.GrossSalary * r.Positions)
                };

                _logger.LogInformation($"✅ Generated job requisition summary: {summary.TotalRequisitions} requisitions");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating job requisition summary");
                return new JobRequisitionSummary();
            }
        }
        #endregion

        #region CREATE Operations
        public async Task<(bool Success, string Message, JobRequisition Data)> CreateJobRequisitionAsync(JobRequisitionCreate model)
        {
            try
            {
                _logger.LogInformation($"🎯 CREATE: Starting new job requisition creation");

                // Validate required fields
                var validationErrors = await ValidateJobRequisitionAsync(model);
                if (validationErrors.Any())
                {
                    var errorMessage = string.Join("; ", validationErrors);
                    _logger.LogWarning($"❌ Validation failed: {errorMessage}");
                    return (false, errorMessage, null);
                }

                var url = GetFullUrl("Job_Requisition_Application");

                // Parse and validate dates
                if (!DateTime.TryParse(model.ApplicationStartDate, out var startDate))
                {
                    return (false, "Error: Invalid Application Start Date format", null);
                }

                if (!DateTime.TryParse(model.ApplicationDeadline, out var deadline))
                {
                    return (false, "Error: Invalid Application Deadline format", null);
                }

                if (!DateTime.TryParse(model.ExpectedReportingDate, out var reportingDate))
                {
                    return (false, "Error: Invalid Expected Reporting Date format", null);
                }

                _logger.LogInformation($"📤 Creating job requisition: Position={model.JobPosition}, Positions={model.Positions}");

                // Prepare payload
                object payload = new
                {
                    Job_Position = model.JobPosition,
                    Employment_Type = model.EmploymentType,
                    Department_Code = model.DepartmentCode,
                    Reason_for_Recruitment = model.ReasonForRecruitment,
                    Positions = model.Positions,
                    Job_Grade = model.JobGrade,
                    Gross_Salary = model.GrossSalary,
                    Contract_Period = model.ContractPeriod,
                    Has_Gratuity = model.HasGratuity,
                    Application_Start_Date = startDate.ToString("yyyy-MM-dd"),
                    Application_Deadline = deadline.ToString("yyyy-MM-dd"),
                    Expected_Reporting_Date = reportingDate.ToString("yyyy-MM-dd"),
                    Requested_By = model.RequestedBy,
                    ShortListing_Required = model.ShortListingRequired,
                    Shortlisting_Threshold = model.ShortlistingThreshold,
                    Status = "Pending",
                    Posted = false
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
                    _logger.LogInformation($"✅ Job requisition created successfully!");

                    try
                    {
                        var createdApplication = JsonConvert.DeserializeObject<JobRequisition>(responseContent);
                        return (true, "Job requisition created successfully", createdApplication);
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning($"⚠️ Could not parse created requisition: {parseEx.Message}");
                        return (true, "Job requisition created successfully (details not retrieved)", null);
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Status Code: {response.StatusCode}");
                    _logger.LogError($"❌ Response: {errorResponse}");

                    var errorMessage = ParseBusinessCentralError(errorResponse);
                    return (false, errorMessage, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception creating job requisition");
                return (false, $"Error: {ex.Message}", null);
            }
        }
        #endregion

        #region UPDATE Operations
        public async Task<(bool Success, string Message)> UpdateJobRequisitionAsync(JobRequisitionUpdate model, string etag)
        {
            try
            {
                _logger.LogInformation($"🔄 UPDATE: Updating job requisition {model.ApplicationNo}");

                if (string.IsNullOrEmpty(model.ApplicationNo))
                    return (false, "Error: Application number is required");

                var url = GetFullUrl($"Job_Requisition_Application('{model.ApplicationNo}')");
                _logger.LogInformation($"📡 Update URL: {url}");

                using var requestClient = new HttpClient();
                requestClient.DefaultRequestHeaders.Clear();
                requestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
                requestClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (!string.IsNullOrEmpty(etag))
                {
                    requestClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                // Prepare update payload
                var updatePayload = new Dictionary<string, object>
                {
                    ["Job_Position"] = model.JobPosition,
                    ["Employment_Type"] = model.EmploymentType,
                    ["Department_Code"] = model.DepartmentCode,
                    ["Reason_for_Recruitment"] = model.ReasonForRecruitment,
                    ["Positions"] = model.Positions,
                    ["Job_Grade"] = model.JobGrade,
                    ["Gross_Salary"] = model.GrossSalary,
                    ["Contract_Period"] = model.ContractPeriod,
                    ["Has_Gratuity"] = model.HasGratuity,
                    ["ShortListing_Required"] = model.ShortListingRequired,
                    ["Shortlisting_Threshold"] = model.ShortlistingThreshold,
                    ["Posted"] = model.Posted
                };

                // Add date fields if provided
                if (!string.IsNullOrEmpty(model.ApplicationStartDate))
                    updatePayload["Application_Start_Date"] = model.ApplicationStartDate;

                if (!string.IsNullOrEmpty(model.ApplicationDeadline))
                    updatePayload["Application_Deadline"] = model.ApplicationDeadline;

                if (!string.IsNullOrEmpty(model.ExpectedReportingDate))
                    updatePayload["Expected_Reporting_Date"] = model.ExpectedReportingDate;

                if (!string.IsNullOrEmpty(model.Status))
                    updatePayload["Status"] = model.Status;

                var jsonContent = JsonConvert.SerializeObject(updatePayload, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-dd"
                });

                _logger.LogInformation($"📤 Update Payload: {jsonContent}");

                var request = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                var response = await requestClient.SendAsync(request);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation($"✅ Job requisition {model.ApplicationNo} updated successfully");
                    return (true, "Job requisition updated successfully");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                {
                    _logger.LogError($"❌ ETag mismatch - record modified by another user");
                    return (false, "Error: This record was modified by another user. Please refresh and try again.");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError($"❌ Job requisition not found: {model.ApplicationNo}");
                    return (false, "Error: Job requisition not found or may have been deleted");
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
                _logger.LogError(ex, "❌ Error updating job requisition");
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateJobRequisitionStatusAsync(string applicationNo, string newStatus, string etag)
        {
            try
            {
                var requisition = await GetJobRequisitionAsync(applicationNo);
                if (requisition == null)
                    return (false, "Error: Job requisition not found");

                var updateModel = new JobRequisitionUpdate
                {
                    ApplicationNo = applicationNo,
                    JobPosition = requisition.JobPosition,
                    EmploymentType = requisition.EmploymentType,
                    DepartmentCode = requisition.DepartmentCode,
                    ReasonForRecruitment = requisition.ReasonForRecruitment,
                    Positions = requisition.Positions,
                    JobGrade = requisition.JobGrade,
                    GrossSalary = requisition.GrossSalary,
                    ContractPeriod = requisition.ContractPeriod,
                    HasGratuity = requisition.HasGratuity,
                    ApplicationStartDate = requisition.ApplicationStartDate,
                    ApplicationDeadline = requisition.ApplicationDeadline,
                    ExpectedReportingDate = requisition.ExpectedReportingDate,
                    RequestedBy = requisition.RequestedBy,
                    Status = newStatus,
                    ShortListingRequired = requisition.ShortListingRequired,
                    ShortlistingThreshold = requisition.ShortlistingThreshold,
                    Posted = requisition.Posted
                };

                return await UpdateJobRequisitionAsync(updateModel, etag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating status for {applicationNo}");
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<bool> PostJobRequisitionAsync(string applicationNo, string etag)
        {
            try
            {
                var result = await UpdateJobRequisitionStatusAsync(applicationNo, "Posted", etag);
                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error posting job requisition {applicationNo}");
                return false;
            }
        }
        #endregion

        #region DELETE Operations
        public async Task<bool> DeleteJobRequisitionAsync(string applicationNo, string etag)
        {
            try
            {
                var url = GetFullUrl($"Job_Requisition_Application('{applicationNo}')");

                _logger.LogInformation($"🗑️ DELETE: {applicationNo}");

                SetupHttpHeaders();

                if (!string.IsNullOrEmpty(etag))
                {
                    _httpClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation($"✅ Job requisition {applicationNo} deleted successfully");
                    return true;
                }

                _logger.LogError($"❌ Delete failed with status {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting job requisition {applicationNo}");
                return false;
            }
        }
        #endregion

        #region VALIDATION Operations
        public async Task<List<string>> ValidateJobRequisitionAsync(JobRequisitionCreate model)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(model.JobPosition))
                errors.Add("Job position is required");

            if (string.IsNullOrEmpty(model.EmploymentType))
                errors.Add("Employment type is required");

            if (string.IsNullOrEmpty(model.DepartmentCode))
                errors.Add("Department code is required");

            if (model.Positions <= 0)
                errors.Add("Number of positions must be greater than 0");

            if (string.IsNullOrEmpty(model.JobGrade))
                errors.Add("Job grade is required");

            if (model.GrossSalary < 0)
                errors.Add("Gross salary cannot be negative");

            if (string.IsNullOrEmpty(model.ApplicationStartDate))
                errors.Add("Application start date is required");

            if (!DateTime.TryParse(model.ApplicationStartDate, out var startDate))
                errors.Add("Invalid Application start date format");

            if (!DateTime.TryParse(model.ApplicationDeadline, out var deadline))
                errors.Add("Invalid Application deadline format");

            if (startDate > deadline)
                errors.Add("Application deadline must be after start date");

            if (!DateTime.TryParse(model.ExpectedReportingDate, out var reportingDate))
                errors.Add("Invalid Expected reporting date format");

            if (string.IsNullOrEmpty(model.RequestedBy))
                errors.Add("Requested by is required");

            if (model.ShortListingRequired && model.ShortlistingThreshold <= 0)
                errors.Add("Shortlisting threshold must be greater than 0 when shortlisting is required");

            return await Task.FromResult(errors);
        }

        public async Task<bool> JobRequisitionExistsAsync(string applicationNo)
        {
            try
            {
                var requisition = await GetJobRequisitionAsync(applicationNo);
                return requisition != null;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
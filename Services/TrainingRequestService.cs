// TrainingRequestService.cs
using KNQASelfService.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace KNQASelfService.Services
{
    public class TrainingRequestService : ITrainingRequestService
    {
        private readonly HttpClient _httpClient;
        private readonly BusinessCentralSettings _settings;
        private readonly ILogger<TrainingRequestService> _logger;
        private readonly string _authHeaderValue;

        public TrainingRequestService(
            IConfiguration configuration,
            ILogger<TrainingRequestService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();

            _settings = new BusinessCentralSettings();
            configuration.GetSection("BusinessCentral").Bind(_settings);

            var credentials = $"{_settings.Username}:{_settings.Password}";
            _authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            _logger.LogInformation($"✅ Training Request Service initialized: {_settings.BaseUrl}");
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
        public async Task<List<TrainingRequest>> GetTrainingRequestsByEmployeeAsync(string employeeNo)
        {
            try
            {
                var url = GetFullUrl($"Training_Request_List?$filter=Employee_No eq '{employeeNo}'&$orderby=Request_Date desc");

                _logger.LogInformation($"📡 GET Training Requests for Employee: {employeeNo}");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<TrainingRequestODataResponse>(content);

                    _logger.LogInformation($"✅ Retrieved {result?.Value?.Count ?? 0} training requests for {employeeNo}");
                    return result?.Value ?? new List<TrainingRequest>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Status: {response.StatusCode}, Error: {error}");
                    return new List<TrainingRequest>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting training requests for employee {employeeNo}");
                return new List<TrainingRequest>();
            }
        }

        public async Task<TrainingRequest> GetTrainingRequestAsync(string requestNo)
        {
            try
            {
                var url = GetFullUrl($"Training_Request_List?$filter=Request_No eq '{requestNo}'");

                _logger.LogInformation($"📡 GET Training Request: {requestNo}");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<TrainingRequestODataResponse>(content);
                    var request = result?.Value?.FirstOrDefault();

                    if (request != null)
                    {
                        _logger.LogInformation($"✅ Retrieved training request {requestNo}");
                    }

                    return request;
                }

                _logger.LogWarning($"⚠️ Training request {requestNo} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting training request {requestNo}");
                return null;
            }
        }

        public async Task<List<TrainingRequest>> GetTrainingRequestsAsync(TrainingRequestFilter filter)
        {
            try
            {
                var filterParts = new List<string>();

                if (!string.IsNullOrEmpty(filter.EmployeeNo))
                    filterParts.Add($"Employee_No eq '{filter.EmployeeNo}'");

                if (!string.IsNullOrEmpty(filter.DepartmentCode))
                    filterParts.Add($"Department_Code eq '{filter.DepartmentCode}'");

                if (!string.IsNullOrEmpty(filter.Status))
                    filterParts.Add($"Status eq '{filter.Status}'");

                if (!string.IsNullOrEmpty(filter.TrainingType))
                    filterParts.Add($"Training_Type eq '{filter.TrainingType}'");

                if (filter.FromDate.HasValue)
                    filterParts.Add($"Request_Date ge {filter.FromDate.Value:yyyy-MM-dd}");

                if (filter.ToDate.HasValue)
                    filterParts.Add($"Request_Date le {filter.ToDate.Value:yyyy-MM-dd}");

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    filterParts.Add($"(contains(Course_Title, '{filter.SearchTerm}') or contains(Request_No, '{filter.SearchTerm}') or contains(Training_Institution, '{filter.SearchTerm}'))");
                }

                var filterQuery = filterParts.Count > 0 ? $"&$filter={string.Join(" and ", filterParts)}" : "";
                var orderBy = $"&$orderby={Uri.EscapeDataString(filter.OrderBy)}";

                var url = GetFullUrl($"Training_Request_List?{filterQuery}{orderBy}");

                _logger.LogInformation($"📡 GET Training Requests with filters");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<TrainingRequestODataResponse>(content);
                    return result?.Value ?? new List<TrainingRequest>();
                }

                return new List<TrainingRequest>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting training requests with filters");
                return new List<TrainingRequest>();
            }
        }

        public async Task<List<TrainingRequest>> GetTrainingRequestsByStatusAsync(string employeeNo, string status)
        {
            var filter = new TrainingRequestFilter
            {
                EmployeeNo = employeeNo,
                Status = status
            };
            return await GetTrainingRequestsAsync(filter);
        }

        public async Task<TrainingRequestSummary> GetTrainingRequestSummaryAsync(string employeeNo)
        {
            try
            {
                var filter = new TrainingRequestFilter { EmployeeNo = employeeNo };
                var requests = await GetTrainingRequestsAsync(filter);

                var summary = new TrainingRequestSummary
                {
                    TotalRequests = requests.Count,
                    PendingRequests = requests.Count(r => r.Status == "Pending"),
                    ApprovedRequests = requests.Count(r => r.Status == "Approved"),
                    RejectedRequests = requests.Count(r => r.Status == "Rejected"),
                    CompletedRequests = requests.Count(r => r.Status == "Completed"),
                    SelfFundedRequests = requests.Count(r => r.SelfFunded),
                    OrganizationFundedRequests = requests.Count(r => !r.SelfFunded),
                    TotalEstimatedCost = requests.Sum(r => r.EstimatedCost)
                };

                _logger.LogInformation($"✅ Generated training request summary for {employeeNo}: {summary.TotalRequests} requests");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error generating training request summary for {employeeNo}");
                return new TrainingRequestSummary();
            }
        }
        #endregion

        #region CREATE Operations
        public async Task<(bool Success, string Message, TrainingRequest Data)> CreateTrainingRequestAsync(TrainingRequestCreate model)
        {
            try
            {
                _logger.LogInformation($"🎯 CREATE: Starting new training request creation for {model.EmployeeNo}");

                // Validate required fields
                var validationErrors = await ValidateTrainingRequestAsync(model);
                if (validationErrors.Any())
                {
                    var errorMessage = string.Join("; ", validationErrors);
                    _logger.LogWarning($"❌ Validation failed: {errorMessage}");
                    return (false, errorMessage, null);
                }

                var url = GetFullUrl("Training_Request_Application");

                // Parse and validate dates
                if (!DateTime.TryParse(model.StartDate, out var startDate))
                {
                    return (false, "Error: Invalid Start Date format", null);
                }

                if (!DateTime.TryParse(model.EndDate, out var endDate))
                {
                    return (false, "Error: Invalid End Date format", null);
                }

                _logger.LogInformation($"📤 Creating training request: Course={model.CourseTitle}, Institution={model.TrainingInstitution}");

                // Prepare payload
                object payload = new
                {
                    Employee_No = model.EmployeeNo,
                    Job_Position = model.JobPosition,
                    Department_Code = model.DepartmentCode,
                    Highest_Academic_Qualification = model.HighestAcademicQualification,
                    Currently_Pursuing_Training = model.CurrentlyPursuingTraining,
                    Training_Institution = model.TrainingInstitution,
                    Course_Title = model.CourseTitle,
                    Sponsoring_Body = model.SponsoringBody,
                    Self_Funded = model.SelfFunded,
                    Start_Date = startDate.ToString("yyyy-MM-dd"),
                    End_Date = endDate.ToString("yyyy-MM-dd"),
                    Course_Duration = model.CourseDuration,
                    Training_Type = model.TrainingType,
                    Training_Mode = model.TrainingMode,
                    Estimated_Cost = model.EstimatedCost,
                    Justification = model.Justification,
                    Expected_Outcomes = model.ExpectedOutcomes,
                    Status = "Pending",
                    Converted_To_Plan = false
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
                    _logger.LogInformation($"✅ Training request created successfully!");

                    try
                    {
                        var createdRequest = JsonConvert.DeserializeObject<TrainingRequest>(responseContent);
                        return (true, "Training request created successfully", createdRequest);
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning($"⚠️ Could not parse created request: {parseEx.Message}");
                        return (true, "Training request created successfully (details not retrieved)", null);
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
                _logger.LogError(ex, "❌ Exception creating training request");
                return (false, $"Error: {ex.Message}", null);
            }
        }
        #endregion

        #region UPDATE Operations
        public async Task<(bool Success, string Message)> UpdateTrainingRequestAsync(TrainingRequestUpdate model, string etag)
        {
            try
            {
                _logger.LogInformation($"🔄 UPDATE: Updating training request {model.RequestNo}");

                if (string.IsNullOrEmpty(model.RequestNo))
                    return (false, "Error: Request number is required");

                var url = GetFullUrl($"Training_Request_Application('{model.RequestNo}')");
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
                    ["Highest_Academic_Qualification"] = model.HighestAcademicQualification,
                    ["Currently_Pursuing_Training"] = model.CurrentlyPursuingTraining,
                    ["Training_Institution"] = model.TrainingInstitution,
                    ["Course_Title"] = model.CourseTitle,
                    ["Sponsoring_Body"] = model.SponsoringBody,
                    ["Self_Funded"] = model.SelfFunded,
                    ["Course_Duration"] = model.CourseDuration,
                    ["Training_Type"] = model.TrainingType,
                    ["Training_Mode"] = model.TrainingMode,
                    ["Estimated_Cost"] = model.EstimatedCost,
                    ["Justification"] = model.Justification,
                    ["Expected_Outcomes"] = model.ExpectedOutcomes,
                    ["Converted_To_Plan"] = model.ConvertedToPlan
                };

                // Add date fields if provided
                if (!string.IsNullOrEmpty(model.StartDate))
                    updatePayload["Start_Date"] = model.StartDate;

                if (!string.IsNullOrEmpty(model.EndDate))
                    updatePayload["End_Date"] = model.EndDate;

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
                    _logger.LogInformation($"✅ Training request {model.RequestNo} updated successfully");
                    return (true, "Training request updated successfully");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                {
                    _logger.LogError($"❌ ETag mismatch - record modified by another user");
                    return (false, "Error: This record was modified by another user. Please refresh and try again.");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError($"❌ Training request not found: {model.RequestNo}");
                    return (false, "Error: Training request not found or may have been deleted");
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
                _logger.LogError(ex, "❌ Error updating training request");
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateTrainingRequestStatusAsync(string requestNo, string newStatus, string etag)
        {
            try
            {
                var request = await GetTrainingRequestAsync(requestNo);
                if (request == null)
                    return (false, "Error: Training request not found");

                var updateModel = new TrainingRequestUpdate
                {
                    RequestNo = requestNo,
                    HighestAcademicQualification = request.HighestAcademicQualification,
                    CurrentlyPursuingTraining = request.CurrentlyPursuingTraining,
                    TrainingInstitution = request.TrainingInstitution,
                    CourseTitle = request.CourseTitle,
                    SponsoringBody = request.SponsoringBody,
                    SelfFunded = request.SelfFunded,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    CourseDuration = request.CourseDuration,
                    TrainingType = request.TrainingType,
                    TrainingMode = request.TrainingMode,
                    EstimatedCost = request.EstimatedCost,
                    Justification = request.Justification,
                    ExpectedOutcomes = request.ExpectedOutcomes,
                    Status = newStatus,
                    ConvertedToPlan = request.ConvertedToPlan
                };

                return await UpdateTrainingRequestAsync(updateModel, etag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating status for {requestNo}");
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<bool> ConvertToTrainingPlanAsync(string requestNo, string etag)
        {
            try
            {
                var result = await UpdateTrainingRequestStatusAsync(requestNo, "Converted", etag);
                if (result.Success)
                {
                    // Additional logic to create a training plan from the request
                    // This would involve calling another service to create a training plan
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error converting training request {requestNo} to plan");
                return false;
            }
        }
        #endregion

        #region DELETE Operations
        public async Task<bool> DeleteTrainingRequestAsync(string requestNo, string etag)
        {
            try
            {
                var url = GetFullUrl($"Training_Request_Application('{requestNo}')");

                _logger.LogInformation($"🗑️ DELETE: {requestNo}");

                SetupHttpHeaders();

                if (!string.IsNullOrEmpty(etag))
                {
                    _httpClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation($"✅ Training request {requestNo} deleted successfully");
                    return true;
                }

                _logger.LogError($"❌ Delete failed with status {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting training request {requestNo}");
                return false;
            }
        }
        #endregion

        #region VALIDATION Operations
        public async Task<List<string>> ValidateTrainingRequestAsync(TrainingRequestCreate model)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(model.EmployeeNo))
                errors.Add("Employee number is required");

            if (string.IsNullOrEmpty(model.JobPosition))
                errors.Add("Job position is required");

            if (string.IsNullOrEmpty(model.DepartmentCode))
                errors.Add("Department code is required");

            if (string.IsNullOrEmpty(model.HighestAcademicQualification))
                errors.Add("Highest academic qualification is required");

            if (string.IsNullOrEmpty(model.TrainingInstitution))
                errors.Add("Training institution is required");

            if (string.IsNullOrEmpty(model.CourseTitle))
                errors.Add("Course title is required");

            if (string.IsNullOrEmpty(model.StartDate))
                errors.Add("Start date is required");

            if (!DateTime.TryParse(model.StartDate, out var startDate))
                errors.Add("Invalid Start date format");

            if (!DateTime.TryParse(model.EndDate, out var endDate))
                errors.Add("Invalid End date format");

            if (startDate >= endDate)
                errors.Add("End date must be after start date");

            if (string.IsNullOrEmpty(model.TrainingType))
                errors.Add("Training type is required");

            if (string.IsNullOrEmpty(model.TrainingMode))
                errors.Add("Training mode is required");

            if (model.EstimatedCost < 0)
                errors.Add("Estimated cost cannot be negative");

            if (string.IsNullOrEmpty(model.Justification) || model.Justification.Length < 50)
                errors.Add("Justification must be at least 50 characters");

            if (string.IsNullOrEmpty(model.ExpectedOutcomes) || model.ExpectedOutcomes.Length < 30)
                errors.Add("Expected outcomes must be at least 30 characters");

            return await Task.FromResult(errors);
        }

        public async Task<bool> TrainingRequestExistsAsync(string requestNo)
        {
            try
            {
                var request = await GetTrainingRequestAsync(requestNo);
                return request != null;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
// TrainingEvaluationService.cs
using KNQASelfService.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace KNQASelfService.Services
{
    public class TrainingEvaluationService : ITrainingEvaluationService
    {
        private readonly HttpClient _httpClient;
        private readonly BusinessCentralSettings _settings;
        private readonly ILogger<TrainingEvaluationService> _logger;
        private readonly string _authHeaderValue;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public TrainingEvaluationService(
            IConfiguration configuration,
            ILogger<TrainingEvaluationService> logger,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
            _httpClient = new HttpClient();

            _settings = new BusinessCentralSettings();
            configuration.GetSection("BusinessCentral").Bind(_settings);

            var credentials = $"{_settings.Username}:{_settings.Password}";
            _authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            _logger.LogInformation($"✅ Training Evaluation Service initialized: {_settings.BaseUrl}");
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
        public async Task<List<TrainingEvaluation>> GetTrainingEvaluationsByEmployeeAsync(string employeeNo)
        {
            try
            {
                var url = GetFullUrl($"Training_Evaluation_List?$filter=Employee_No eq '{employeeNo}'&$orderby=Evaluation_Date desc");

                _logger.LogInformation($"📡 GET Training Evaluations for Employee: {employeeNo}");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<TrainingEvaluationODataResponse>(content);

                    _logger.LogInformation($"✅ Retrieved {result?.Value?.Count ?? 0} training evaluations for {employeeNo}");
                    return result?.Value ?? new List<TrainingEvaluation>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Status: {response.StatusCode}, Error: {error}");
                    return new List<TrainingEvaluation>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting training evaluations for employee {employeeNo}");
                return new List<TrainingEvaluation>();
            }
        }

        public async Task<TrainingEvaluation> GetTrainingEvaluationAsync(string evaluationNo)
        {
            try
            {
                var url = GetFullUrl($"Training_Evaluation_List?$filter=Evaluation_No eq '{evaluationNo}'");

                _logger.LogInformation($"📡 GET Training Evaluation: {evaluationNo}");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<TrainingEvaluationODataResponse>(content);
                    var evaluation = result?.Value?.FirstOrDefault();

                    if (evaluation != null)
                    {
                        _logger.LogInformation($"✅ Retrieved training evaluation {evaluationNo}");
                    }

                    return evaluation;
                }

                _logger.LogWarning($"⚠️ Training evaluation {evaluationNo} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting training evaluation {evaluationNo}");
                return null;
            }
        }

        public async Task<List<TrainingEvaluation>> GetTrainingEvaluationsAsync(TrainingEvaluationFilter filter)
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
                    filterParts.Add($"Evaluation_Date ge {filter.FromDate.Value:yyyy-MM-dd}");

                if (filter.ToDate.HasValue)
                    filterParts.Add($"Evaluation_Date le {filter.ToDate.Value:yyyy-MM-dd}");

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    filterParts.Add($"(contains(Course_Title, '{filter.SearchTerm}') or contains(Evaluation_No, '{filter.SearchTerm}') or contains(Employee_Name, '{filter.SearchTerm}'))");
                }

                var filterQuery = filterParts.Count > 0 ? $"&$filter={string.Join(" and ", filterParts)}" : "";
                var orderBy = $"&$orderby={Uri.EscapeDataString(filter.OrderBy)}";

                var url = GetFullUrl($"Training_Evaluation_List?{filterQuery}{orderBy}");

                _logger.LogInformation($"📡 GET Training Evaluations with filters");

                SetupHttpHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<TrainingEvaluationODataResponse>(content);
                    return result?.Value ?? new List<TrainingEvaluation>();
                }

                return new List<TrainingEvaluation>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting training evaluations with filters");
                return new List<TrainingEvaluation>();
            }
        }

        public async Task<List<TrainingEvaluation>> GetEvaluationsByTrainingCodeAsync(string trainingCode)
        {
            var filter = new TrainingEvaluationFilter
            {
                SearchTerm = trainingCode
            };
            return await GetTrainingEvaluationsAsync(filter);
        }

        public async Task<TrainingEvaluationSummary> GetTrainingEvaluationSummaryAsync(string employeeNo)
        {
            try
            {
                var filter = new TrainingEvaluationFilter { EmployeeNo = employeeNo };
                var evaluations = await GetTrainingEvaluationsAsync(filter);

                var summary = new TrainingEvaluationSummary
                {
                    TotalEvaluations = evaluations.Count,
                    PendingEvaluations = evaluations.Count(e => e.Status == "Pending" || e.Status == "Open"),
                    SubmittedEvaluations = evaluations.Count(e => e.Status == "Submitted"),
                    ApprovedEvaluations = evaluations.Count(e => e.Status == "Approved"),
                    AverageRating = evaluations.Count > 0 ? (decimal)evaluations.Average(e => e.OverallRating) : 0,
                    WithCertificates = evaluations.Count(e => e.CertificateReceived),
                    WouldRecommendCount = evaluations.Count(e => e.WouldRecommend),
                    MetObjectivesCount = evaluations.Count(e => e.MetObjectives)
                };

                _logger.LogInformation($"✅ Generated training evaluation summary for {employeeNo}: {summary.TotalEvaluations} evaluations");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error generating training evaluation summary for {employeeNo}");
                return new TrainingEvaluationSummary();
            }
        }
        #endregion

        #region CREATE Operations
        public async Task<(bool Success, string Message, TrainingEvaluation Data)> CreateTrainingEvaluationAsync(TrainingEvaluationCreate model)
        {
            try
            {
                _logger.LogInformation($"🎯 CREATE: Starting new training evaluation creation");

                // Validate required fields
                var validationErrors = await ValidateTrainingEvaluationAsync(model);
                if (validationErrors.Any())
                {
                    var errorMessage = string.Join("; ", validationErrors);
                    _logger.LogWarning($"❌ Validation failed: {errorMessage}");
                    return (false, errorMessage, null);
                }

                var url = GetFullUrl("Training_Evaluation_Application");

                _logger.LogInformation($"📤 Creating training evaluation for Training Code: {model.TrainingCode}");

                // Prepare payload
                object payload = new
                {
                    Training_Code = model.TrainingCode,
                    Overall_Rating = model.OverallRating,
                    Trainer_Effectiveness = model.TrainerEffectiveness,
                    Content_Relevance = model.ContentRelevance,
                    Training_Materials = model.TrainingMaterials,
                    Training_Facilities = model.TrainingFacilities,
                    Training_Duration = model.TrainingDuration,
                    Met_Objectives = model.MetObjectives,
                    Would_Recommend = model.WouldRecommend,
                    What_Liked_Most = model.WhatLikedMost,
                    What_Could_Improve = model.WhatCouldImprove,
                    Additional_Comments = model.AdditionalComments,
                    Skill_Improvement_Rating = model.SkillImprovementRating,
                    Applicable_to_Work = model.ApplicableToWork,
                    Implementation_Plan = model.ImplementationPlan,
                    Expected_Impact = model.ExpectedImpact,
                    Certificate_Received = model.CertificateReceived,
                    Status = "Draft"
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
                    _logger.LogInformation($"✅ Training evaluation created successfully!");

                    try
                    {
                        var createdEvaluation = JsonConvert.DeserializeObject<TrainingEvaluation>(responseContent);

                        // If certificate file was provided, upload it
                        if (model.CertificateReceived && model.CertificateFile != null)
                        {
                            var uploadResult = await UploadCertificateAsync(createdEvaluation.EvaluationNo, model.CertificateFile);
                            if (uploadResult.Success)
                            {
                                _logger.LogInformation($"✅ Certificate uploaded: {uploadResult.FileName}");
                            }
                        }

                        return (true, "Training evaluation created successfully", createdEvaluation);
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning($"⚠️ Could not parse created evaluation: {parseEx.Message}");
                        return (true, "Training evaluation created successfully (details not retrieved)", null);
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
                _logger.LogError(ex, "❌ Exception creating training evaluation");
                return (false, $"Error: {ex.Message}", null);
            }
        }
        #endregion

        #region UPDATE Operations
        public async Task<(bool Success, string Message)> UpdateTrainingEvaluationAsync(TrainingEvaluationUpdate model, string etag)
        {
            try
            {
                _logger.LogInformation($"🔄 UPDATE: Updating training evaluation {model.EvaluationNo}");

                if (string.IsNullOrEmpty(model.EvaluationNo))
                    return (false, "Error: Evaluation number is required");

                var url = GetFullUrl($"Training_Evaluation_Application('{model.EvaluationNo}')");
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
                    ["Overall_Rating"] = model.OverallRating,
                    ["Trainer_Effectiveness"] = model.TrainerEffectiveness,
                    ["Content_Relevance"] = model.ContentRelevance,
                    ["Training_Materials"] = model.TrainingMaterials,
                    ["Training_Facilities"] = model.TrainingFacilities,
                    ["Training_Duration"] = model.TrainingDuration,
                    ["Met_Objectives"] = model.MetObjectives,
                    ["Would_Recommend"] = model.WouldRecommend,
                    ["What_Liked_Most"] = model.WhatLikedMost,
                    ["What_Could_Improve"] = model.WhatCouldImprove,
                    ["Additional_Comments"] = model.AdditionalComments,
                    ["Skill_Improvement_Rating"] = model.SkillImprovementRating,
                    ["Applicable_to_Work"] = model.ApplicableToWork,
                    ["Implementation_Plan"] = model.ImplementationPlan,
                    ["Expected_Impact"] = model.ExpectedImpact,
                    ["Certificate_Received"] = model.CertificateReceived,
                    ["Certificate_File_Name"] = model.CertificateFileName,
                    ["Certificate_File_Url"] = model.CertificateFileUrl,
                    ["Certificate_File_Size"] = model.CertificateFileSize
                };

                if (!string.IsNullOrEmpty(model.Status))
                    updatePayload["Status"] = model.Status;

                var jsonContent = JsonConvert.SerializeObject(updatePayload, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                _logger.LogInformation($"📤 Update Payload: {jsonContent}");

                var request = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                var response = await requestClient.SendAsync(request);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation($"✅ Training evaluation {model.EvaluationNo} updated successfully");
                    return (true, "Training evaluation updated successfully");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                {
                    _logger.LogError($"❌ ETag mismatch - record modified by another user");
                    return (false, "Error: This record was modified by another user. Please refresh and try again.");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError($"❌ Training evaluation not found: {model.EvaluationNo}");
                    return (false, "Error: Training evaluation not found or may have been deleted");
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
                _logger.LogError(ex, "❌ Error updating training evaluation");
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateTrainingEvaluationStatusAsync(string evaluationNo, string newStatus, string etag)
        {
            try
            {
                var evaluation = await GetTrainingEvaluationAsync(evaluationNo);
                if (evaluation == null)
                    return (false, "Error: Training evaluation not found");

                var updateModel = new TrainingEvaluationUpdate
                {
                    EvaluationNo = evaluationNo,
                    OverallRating = evaluation.OverallRating,
                    TrainerEffectiveness = evaluation.TrainerEffectiveness,
                    ContentRelevance = evaluation.ContentRelevance,
                    TrainingMaterials = evaluation.TrainingMaterials,
                    TrainingFacilities = evaluation.TrainingFacilities,
                    TrainingDuration = evaluation.TrainingDuration,
                    MetObjectives = evaluation.MetObjectives,
                    WouldRecommend = evaluation.WouldRecommend,
                    WhatLikedMost = evaluation.WhatLikedMost,
                    WhatCouldImprove = evaluation.WhatCouldImprove,
                    AdditionalComments = evaluation.AdditionalComments,
                    SkillImprovementRating = evaluation.SkillImprovementRating,
                    ApplicableToWork = evaluation.ApplicableToWork,
                    ImplementationPlan = evaluation.ImplementationPlan,
                    ExpectedImpact = evaluation.ExpectedImpact,
                    Status = newStatus,
                    CertificateReceived = evaluation.CertificateReceived,
                    CertificateFileName = evaluation.CertificateFileName,
                    CertificateFileUrl = evaluation.CertificateFileUrl,
                    CertificateFileSize = evaluation.CertificateFileSize
                };

                return await UpdateTrainingEvaluationAsync(updateModel, etag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating status for {evaluationNo}");
                return (false, $"Error: {ex.Message}");
            }
        }
        #endregion

        #region DELETE Operations
        public async Task<bool> DeleteTrainingEvaluationAsync(string evaluationNo, string etag)
        {
            try
            {
                var url = GetFullUrl($"Training_Evaluation_Application('{evaluationNo}')");

                _logger.LogInformation($"🗑️ DELETE: {evaluationNo}");

                SetupHttpHeaders();

                if (!string.IsNullOrEmpty(etag))
                {
                    _httpClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation($"✅ Training evaluation {evaluationNo} deleted successfully");
                    return true;
                }

                _logger.LogError($"❌ Delete failed with status {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting training evaluation {evaluationNo}");
                return false;
            }
        }
        #endregion

        #region FILE UPLOAD Operations
        public async Task<FileUploadResponse> UploadCertificateAsync(string evaluationNo, IBrowserFile file)
        {
            try
            {
                if (file == null)
                    return new FileUploadResponse { Success = false, Message = "No file provided" };

                // Validate file size (max 10MB)
                if (file.Size > 10 * 1024 * 1024)
                    return new FileUploadResponse { Success = false, Message = "File size exceeds 10MB limit" };

                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(file.Name).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                    return new FileUploadResponse { Success = false, Message = "File type not allowed. Allowed: PDF, JPG, PNG, DOC, DOCX" };

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "certificates");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileName = $"{evaluationNo}_{Guid.NewGuid():N}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(stream);
                }

                // In a real application, you might want to:
                // 1. Upload to cloud storage (Azure Blob, AWS S3)
                // 2. Store file metadata in database
                // 3. Create thumbnails for images

                var fileUrl = $"/uploads/certificates/{fileName}";

                _logger.LogInformation($"✅ Certificate uploaded: {fileName} ({file.Size} bytes)");

                return new FileUploadResponse
                {
                    Success = true,
                    FileName = fileName,
                    FileUrl = fileUrl,
                    FileSize = file.Size,
                    Message = "Certificate uploaded successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error uploading certificate");
                return new FileUploadResponse { Success = false, Message = $"Error uploading file: {ex.Message}" };
            }
        }

        public async Task<bool> DeleteCertificateAsync(string evaluationNo, string fileName)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", "certificates", fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation($"✅ Certificate deleted: {fileName}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting certificate {fileName}");
                return false;
            }
        }

        public async Task<byte[]> DownloadCertificateAsync(string fileUrl)
        {
            try
            {
                // Remove leading slash if present
                var relativePath = fileUrl.StartsWith("/") ? fileUrl.Substring(1) : fileUrl;
                var filePath = Path.Combine(_environment.WebRootPath, relativePath);

                if (File.Exists(filePath))
                {
                    return await File.ReadAllBytesAsync(filePath);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error downloading certificate from {fileUrl}");
                return null;
            }
        }
        #endregion

        #region VALIDATION Operations
        public async Task<List<string>> ValidateTrainingEvaluationAsync(TrainingEvaluationCreate model)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(model.TrainingCode))
                errors.Add("Training code is required");

            if (model.OverallRating < 1 || model.OverallRating > 5)
                errors.Add("Overall rating must be between 1 and 5");

            if (string.IsNullOrEmpty(model.TrainerEffectiveness))
                errors.Add("Trainer effectiveness rating is required");

            if (string.IsNullOrEmpty(model.ContentRelevance))
                errors.Add("Content relevance rating is required");

            if (string.IsNullOrEmpty(model.TrainingMaterials))
                errors.Add("Training materials rating is required");

            if (string.IsNullOrEmpty(model.TrainingDuration))
                errors.Add("Training duration rating is required");

            if (model.SkillImprovementRating < 1 || model.SkillImprovementRating > 5)
                errors.Add("Skill improvement rating must be between 1 and 5");

            if (string.IsNullOrEmpty(model.ApplicableToWork))
                errors.Add("Applicability to work rating is required");

            if (string.IsNullOrEmpty(model.WhatLikedMost) || model.WhatLikedMost.Length < 10)
                errors.Add("What you liked most must be at least 10 characters");

            if (string.IsNullOrEmpty(model.WhatCouldImprove) || model.WhatCouldImprove.Length < 10)
                errors.Add("What could be improved must be at least 10 characters");

            // Validate certificate file if provided
            if (model.CertificateReceived && model.CertificateFile != null)
            {
                if (model.CertificateFile.Size > 10 * 1024 * 1024)
                    errors.Add("Certificate file size exceeds 10MB limit");

                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(model.CertificateFile.Name).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                    errors.Add("Certificate file type not allowed. Allowed: PDF, JPG, PNG, DOC, DOCX");
            }

            return await Task.FromResult(errors);
        }

        public async Task<bool> TrainingEvaluationExistsAsync(string evaluationNo)
        {
            try
            {
                var evaluation = await GetTrainingEvaluationAsync(evaluationNo);
                return evaluation != null;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
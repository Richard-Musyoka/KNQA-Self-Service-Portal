using KNQASelfService.Models;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace KNQASelfService.Services
{
    public class PerformanceTargetService : IPerformanceTargetService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PerformanceTargetService> _logger;
        private readonly IEmployeeService _employeeService;
        private readonly string _baseUrl;
        private readonly string _authHeaderValue;

        public PerformanceTargetService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<PerformanceTargetService> logger,
            IEmployeeService employeeService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _employeeService = employeeService;

            _baseUrl = _configuration["BusinessCentral:BaseUrl"] ?? "";
            var username = _configuration["BusinessCentral:Username"] ?? "";
            var password = _configuration["BusinessCentral:Password"] ?? "";

            _authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        }
        public async Task<List<PerformanceTarget>> GetPerformanceTargetsByEmployeeAsync(string employeeNo)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/Performance_Targets?$filter=EmployeeNo eq '{employeeNo}'&$orderby=CreatedDate desc");

                SetupRequestHeaders();

                _logger.LogInformation($"🔍 Fetching performance targets for employee {employeeNo} from: {url}");

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var result = JsonConvert.DeserializeObject<ODataResponse<PerformanceTarget>>(content);
                        if (result?.Value != null)
                        {
                            _logger.LogInformation($"✅ Successfully loaded {result.Value.Count} performance targets for employee {employeeNo}");
                            return result.Value;
                        }
                    }
                    catch (Newtonsoft.Json.JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "❌ Error deserializing performance targets");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to fetch performance targets. Status: {response.StatusCode}");
                    _logger.LogError($"❌ Error response: {errorContent}");
                }

                return new List<PerformanceTarget>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching performance targets for employee {employeeNo}");
                return new List<PerformanceTarget>();
            }
        }
        #region Helper Methods

        private string GetFullUrl(string endpoint)
        {
            return $"{_baseUrl}/{endpoint}";
        }

        private void SetupRequestHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private string BuildFilterQuery(PerformanceTargetFilter filter)
        {
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.Replace("'", "''");
                filters.Add($"(contains(ObjectiveNo, '{searchTerm}') or contains(AppraiseeName, '{searchTerm}') or contains(AppraiserName, '{searchTerm}'))");
            }

            if (!string.IsNullOrWhiteSpace(filter.EmployeeNo))
            {
                filters.Add($"EmployeeNo eq '{filter.EmployeeNo.Replace("'", "''")}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.AppraiserNo))
            {
                filters.Add($"AppraiserNo eq '{filter.AppraiserNo.Replace("'", "''")}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                filters.Add($"Status eq '{filter.Status.Replace("'", "''")}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.AppraisalCategory))
            {
                filters.Add($"AppraisalCategory eq '{filter.AppraisalCategory.Replace("'", "''")}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.AppraisalPeriod))
            {
                filters.Add($"AppraisalPeriod eq '{filter.AppraisalPeriod.Replace("'", "''")}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.DepartmentCode))
            {
                filters.Add($"DepartmentCode eq '{filter.DepartmentCode.Replace("'", "''")}'");
            }

            var query = "?";
            if (filters.Any())
            {
                query += "$filter=" + string.Join(" and ", filters);
            }

            if (!string.IsNullOrWhiteSpace(filter.OrderBy))
            {
                query += (filters.Any() ? "&" : "") + "$orderby=" + filter.OrderBy;
            }

            if (!query.Contains("$top"))
            {
                query += (query.Contains("?") ? "&" : "?") + "$top=1000";
            }

            return query == "?" ? "" : query;
        }

        private string ParseErrorResponse(string responseContent)
        {
            try
            {
                var errorResponse = JsonConvert.DeserializeObject<BCErrorResponse>(responseContent);
                return $"Error: {errorResponse?.Error?.Message ?? responseContent}";
            }
            catch
            {
                return $"Error: {responseContent}";
            }
        }

        private async Task<string> GenerateObjectiveNo()
        {
            try
            {
                var currentYear = DateTime.Now.ToString("yyyy");
                var url = GetFullUrl($"Company('KNQA')/PerformanceTargets?$filter=startswith(ObjectiveNo, 'APR') and contains(ObjectiveNo, '{currentYear}')&$top=1&$orderby=ObjectiveNo desc");

                SetupRequestHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<PerformanceTarget>>(content);

                    if (result?.Value != null && result.Value.Any())
                    {
                        var lastNo = result.Value.First().ObjectiveNo;
                        if (lastNo != null && lastNo.StartsWith("APR"))
                        {
                            var parts = lastNo.Split('.');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
                            {
                                return $"APR{currentYear.Substring(2)}.{lastNumber + 1:00000}";
                            }
                        }
                    }
                }

                // Default format if no existing records
                return $"APR{DateTime.Now:yy}.00001";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating objective number");
                return $"APR{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        #endregion

        #region CRUD Operations

        public async Task<List<PerformanceTarget>> GetPerformanceTargetsAsync(PerformanceTargetFilter filter)
        {
            try
            {
                var queryParams = BuildFilterQuery(filter);
                var url = GetFullUrl($"Company('KNQA')/PerformanceTargets{queryParams}");

                SetupRequestHeaders();

                _logger.LogInformation($"🔍 Fetching performance targets from: {url}");

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"✅ Performance Targets API response received. Content length: {content.Length}");

                    try
                    {
                        var result = JsonConvert.DeserializeObject<ODataResponse<PerformanceTarget>>(content);
                        if (result?.Value != null)
                        {
                            _logger.LogInformation($"✅ Successfully loaded {result.Value.Count} performance targets");
                            return result.Value;
                        }
                    }
                    catch (Newtonsoft.Json.JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "❌ Error deserializing performance targets");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to fetch performance targets. Status: {response.StatusCode}");
                    _logger.LogError($"❌ Error response: {errorContent}");
                }

                return new List<PerformanceTarget>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching performance targets");
                return new List<PerformanceTarget>();
            }
        }

        public async Task<Models.PerformanceTarget?> GetPerformanceTargetAsync(string objectiveNo)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/PerformanceTargets('{objectiveNo}')");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var target = JsonConvert.DeserializeObject<PerformanceTarget>(content);

                        if (response.Headers.TryGetValues("ETag", out var etagValues))
                        {
                            var etag = etagValues.FirstOrDefault();
                            if (!string.IsNullOrEmpty(etag))
                            {
                                etag = etag.Trim('"');
                                if (target != null) target.ODataEtag = etag;
                            }
                        }

                        return target;
                    }
                    catch (Newtonsoft.Json.JsonException)
                    {
                        return null;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching performance target {objectiveNo}");
                return null;
            }
        }

        public async Task<string> CreatePerformanceTargetAsync(PerformanceTarget target)
        {
            try
            {
                // Generate objective number if not provided
                if (string.IsNullOrEmpty(target.ObjectiveNo))
                {
                    target.ObjectiveNo = await GenerateObjectiveNo();
                }

                // Set created date
                target.CreatedDate = DateTime.Now;
                target.Status = AppraisalStatus.DRAFT;

                var url = GetFullUrl("Company('KNQA')/PerformanceTargets");

                SetupRequestHeaders();

                var json = JsonConvert.SerializeObject(new
                {
                    ObjectiveNo = target.ObjectiveNo,
                    EmployeeNo = target.EmployeeNo,
                    AppraiseeName = target.AppraiseeName,
                    AppraiseeID = target.AppraiseeID,
                    AppraiseeJobID = target.AppraiseeJobID,
                    AppraiseeJobTitle = target.AppraiseeJobTitle,
                    DepartmentCode = target.DepartmentCode,
                    DepartmentName = target.DepartmentName,
                    AppraiserNo = target.AppraiserNo,
                    AppraiserName = target.AppraiserName,
                    AppraiserJobTitle = target.AppraiserJobTitle,
                    Status = target.Status,
                    AppraisalCategory = target.AppraisalCategory,
                    AppraisalPeriod = target.AppraisalPeriod,
                    AgreedPerformanceCategory = target.AgreedPerformanceCategory,
                    Approved = target.Approved,
                    CreatedDate = target.CreatedDate?.ToString("yyyy-MM-dd"),
                    Remarks = target.Remarks
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"📤 Creating performance target: {target.ObjectiveNo}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Performance target {target.ObjectiveNo} created successfully");
                    return $"Success: Performance target {target.ObjectiveNo} created successfully";
                }
                else
                {
                    _logger.LogError($"❌ Failed to create performance target. Status: {response.StatusCode}, Response: {responseContent}");
                    return ParseErrorResponse(responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error creating performance target {target.ObjectiveNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdatePerformanceTargetAsync(PerformanceTarget target)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/PerformanceTargets('{target.ObjectiveNo}')");

                SetupRequestHeaders();

                var json = JsonConvert.SerializeObject(new
                {
                    EmployeeNo = target.EmployeeNo,
                    AppraiseeName = target.AppraiseeName,
                    AppraiseeID = target.AppraiseeID,
                    AppraiseeJobID = target.AppraiseeJobID,
                    AppraiseeJobTitle = target.AppraiseeJobTitle,
                    DepartmentCode = target.DepartmentCode,
                    DepartmentName = target.DepartmentName,
                    AppraiserNo = target.AppraiserNo,
                    AppraiserName = target.AppraiserName,
                    AppraiserJobTitle = target.AppraiserJobTitle,
                    Status = target.Status,
                    AppraisalCategory = target.AppraisalCategory,
                    AppraisalPeriod = target.AppraisalPeriod,
                    AgreedPerformanceCategory = target.AgreedPerformanceCategory,
                    Approved = target.Approved,
                    SubmittedDate = target.SubmittedDate?.ToString("yyyy-MM-dd"),
                    ApprovedDate = target.ApprovedDate?.ToString("yyyy-MM-dd"),
                    Remarks = target.Remarks
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Add If-Match header for concurrency control
                if (!string.IsNullOrEmpty(target.ODataEtag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", target.ODataEtag);
                }

                _logger.LogInformation($"🔄 Updating performance target: {target.ObjectiveNo}");

                var response = await _httpClient.PatchAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Performance target {target.ObjectiveNo} updated successfully");
                    return $"Success: Performance target {target.ObjectiveNo} updated successfully";
                }
                else
                {
                    _logger.LogError($"❌ Failed to update performance target. Status: {response.StatusCode}, Response: {responseContent}");
                    return ParseErrorResponse(responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating performance target {target.ObjectiveNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> DeletePerformanceTargetAsync(string objectiveNo)
        {
            try
            {
                _logger.LogInformation($"🗑️ Deleting performance target: {objectiveNo}");

                var target = await GetPerformanceTargetAsync(objectiveNo);
                if (target == null)
                {
                    return $"Error: Performance target {objectiveNo} not found";
                }

                // Don't allow deletion if not in draft status
                if (target.Status != AppraisalStatus.DRAFT)
                {
                    return $"Error: Cannot delete performance target with status '{target.Status}'. Only draft targets can be deleted.";
                }

                var url = GetFullUrl($"Company('KNQA')/PerformanceTargets('{objectiveNo}')");

                SetupRequestHeaders();

                if (!string.IsNullOrEmpty(target.ODataEtag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", target.ODataEtag);
                }

                var response = await _httpClient.DeleteAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Performance target {objectiveNo} deleted successfully");
                    return $"Success: Performance target {objectiveNo} deleted successfully";
                }
                else
                {
                    _logger.LogError($"❌ Failed to delete performance target. Status: {response.StatusCode}, Response: {responseContent}");
                    return ParseErrorResponse(responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting performance target {objectiveNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> SubmitForApprovalAsync(string objectiveNo)
        {
            try
            {
                var target = await GetPerformanceTargetAsync(objectiveNo);
                if (target == null)
                {
                    return $"Error: Performance target {objectiveNo} not found";
                }

                // Validate before submission
                if (target.Status != AppraisalStatus.DRAFT)
                {
                    return $"Error: Only draft targets can be submitted for approval. Current status is '{target.Status}'";
                }

                if (string.IsNullOrEmpty(target.AppraiserNo))
                {
                    return "Error: Appraiser must be assigned before submission";
                }

                if (target.TargetLines == null || !target.TargetLines.Any())
                {
                    return "Error: At least one target line is required before submission";
                }

                // Calculate total weighting
                var totalWeighting = target.TargetLines.Sum(t => t.Weighting);
                if (totalWeighting != 100)
                {
                    return $"Error: Total weighting must be 100%. Current total is {totalWeighting}%";
                }

                // Update status
                target.Status = AppraisalStatus.PENDING_APPROVAL;
                target.SubmittedDate = DateTime.Now;

                return await UpdatePerformanceTargetAsync(target);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error submitting target {objectiveNo} for approval");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> ApproveTargetAsync(string objectiveNo, string approverComments = "")
        {
            try
            {
                var target = await GetPerformanceTargetAsync(objectiveNo);
                if (target == null)
                {
                    return $"Error: Performance target {objectiveNo} not found";
                }

                if (target.Status != AppraisalStatus.PENDING_APPROVAL)
                {
                    return $"Error: Only targets with 'Pending Approval' status can be approved. Current status is '{target.Status}'";
                }

                target.Status = AppraisalStatus.APPROVED;
                target.Approved = true;
                target.ApprovedDate = DateTime.Now;

                if (!string.IsNullOrEmpty(approverComments))
                {
                    target.Remarks = approverComments;
                }

                return await UpdatePerformanceTargetAsync(target);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error approving target {objectiveNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> RejectTargetAsync(string objectiveNo, string rejectionReason = "")
        {
            try
            {
                var target = await GetPerformanceTargetAsync(objectiveNo);
                if (target == null)
                {
                    return $"Error: Performance target {objectiveNo} not found";
                }

                if (target.Status != AppraisalStatus.PENDING_APPROVAL)
                {
                    return $"Error: Only targets with 'Pending Approval' status can be rejected. Current status is '{target.Status}'";
                }

                if (string.IsNullOrEmpty(rejectionReason))
                {
                    return "Error: Rejection reason is required";
                }

                target.Status = AppraisalStatus.REJECTED;
                target.Remarks = rejectionReason;

                return await UpdatePerformanceTargetAsync(target);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error rejecting target {objectiveNo}");
                return $"Error: {ex.Message}";
            }
        }

        #endregion

        #region Lookup Data

        public async Task<List<AppraisalCategory>> GetAppraisalCategoriesAsync()
        {
            try
            {
                var url = GetFullUrl("Company('KNQA')/Appraisal_Categories?$filter=Active eq true&$select=Code,Description,Active");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<AppraisalCategory>>(content);
                    return result?.Value ?? new List<AppraisalCategory>();
                }

                // Fallback categories
                return new List<AppraisalCategory>
                {
                    new AppraisalCategory { Code = "TARGETSET", Description = "Target Setting", Active = true },
                    new AppraisalCategory { Code = "MIDYEAR", Description = "Mid-Year Review", Active = true },
                    new AppraisalCategory { Code = "ANNUAL", Description = "Annual Review", Active = true },
                    new AppraisalCategory { Code = "PROBATION", Description = "Probation Review", Active = true },
                    new AppraisalCategory { Code = "PROMOTION", Description = "Promotion Review", Active = true }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching appraisal categories");
                return new List<AppraisalCategory>();
            }
        }

        public async Task<List<PerformanceCategory>> GetPerformanceCategoriesAsync()
        {
            try
            {
                var url = GetFullUrl("Company('KNQA')/Performance_Categories?$select=Code,Description,MinimumScore,MaximumScore");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<PerformanceCategory>>(content);
                    return result?.Value ?? new List<PerformanceCategory>();
                }

                // Fallback performance categories
                return new List<PerformanceCategory>
                {
                    new PerformanceCategory { Code = "EXCELLENT", Description = "Excellent", MinimumScore = 85, MaximumScore = 100 },
                    new PerformanceCategory { Code = "VERY_GOOD", Description = "Very Good", MinimumScore = 70, MaximumScore = 84 },
                    new PerformanceCategory { Code = "GOOD", Description = "Good", MinimumScore = 55, MaximumScore = 69 },
                    new PerformanceCategory { Code = "FAIR", Description = "Fair", MinimumScore = 40, MaximumScore = 54 },
                    new PerformanceCategory { Code = "POOR", Description = "Poor", MinimumScore = 0, MaximumScore = 39 }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching performance categories");
                return new List<PerformanceCategory>();
            }
        }

        public async Task<List<string>> GetAppraisalPeriodsAsync()
        {
            // Generate appraisal periods (last 5 years and next 2 years)
            var periods = new List<string>();
            var currentYear = DateTime.Now.Year;

            for (int year = currentYear - 2; year <= currentYear + 1; year++)
            {
                periods.Add($"{year}/{year + 1}");
            }

            return await Task.FromResult(periods.OrderByDescending(p => p).ToList());
        }

        public async Task<List<string>> GetAppraisalStatusListAsync()
        {
            return await Task.FromResult(new List<string>
            {
                AppraisalStatus.DRAFT,
                AppraisalStatus.PENDING_APPROVAL,
                AppraisalStatus.APPROVED,
                AppraisalStatus.REJECTED,
                AppraisalStatus.UNDER_REVIEW
            });
        }

        #endregion

        #region Employee-specific Operations

        public async Task<List<PerformanceTarget>> GetTargetsByAppraiseeAsync(string employeeNo)
        {
            try
            {
                var filter = new PerformanceTargetFilter
                {
                    EmployeeNo = employeeNo,
                    OrderBy = "CreatedDate desc"
                };

                return await GetPerformanceTargetsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching targets for appraisee {employeeNo}");
                return new List<PerformanceTarget>();
            }
        }

        public async Task<List<PerformanceTarget>> GetTargetsByAppraiserAsync(string appraiserNo)
        {
            try
            {
                var filter = new PerformanceTargetFilter
                {
                    AppraiserNo = appraiserNo,
                    OrderBy = "CreatedDate desc"
                };

                return await GetPerformanceTargetsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching targets for appraiser {appraiserNo}");
                return new List<PerformanceTarget>();
            }
        }

        public async Task<List<PerformanceTarget>> GetMyDraftTargetsAsync(string employeeNo)
        {
            try
            {
                var filter = new PerformanceTargetFilter
                {
                    EmployeeNo = employeeNo,
                    Status = AppraisalStatus.DRAFT,
                    OrderBy = "CreatedDate desc"
                };

                return await GetPerformanceTargetsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching draft targets for employee {employeeNo}");
                return new List<PerformanceTarget>();
            }
        }

        public async Task<List<PerformanceTarget>> GetMyPendingApprovalTargetsAsync(string employeeNo)
        {
            try
            {
                var filter = new PerformanceTargetFilter
                {
                    EmployeeNo = employeeNo,
                    Status = AppraisalStatus.PENDING_APPROVAL,
                    OrderBy = "CreatedDate desc"
                };

                return await GetPerformanceTargetsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching pending approval targets for employee {employeeNo}");
                return new List<PerformanceTarget>();
            }
        }

        public async Task<List<PerformanceTarget>> GetTargetsPendingMyApprovalAsync(string appraiserNo)
        {
            try
            {
                var filter = new PerformanceTargetFilter
                {
                    AppraiserNo = appraiserNo,
                    Status = AppraisalStatus.PENDING_APPROVAL,
                    OrderBy = "CreatedDate desc"
                };

                return await GetPerformanceTargetsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching targets pending approval for appraiser {appraiserNo}");
                return new List<PerformanceTarget>();
            }
        }

        public async Task<List<Employee>> GetPotentialAppraisersAsync(string employeeNo)
        {
            try
            {
                // Get all active employees except the current user
                var allEmployees = await _employeeService.GetActiveEmployeesAsync();

                // Filter out the current employee
                var potentialAppraisers = allEmployees
                    .Where(e => e.No != employeeNo && e.Status == "Active")
                    .OrderBy(e => e.FullName)
                    .ToList();

                return potentialAppraisers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching potential appraisers");
                return new List<Employee>();
            }
        }

        #endregion

        #region Summary and Reporting

        public async Task<PerformanceTargetSummary> GetTargetSummaryAsync(string? employeeNo = null)
        {
            try
            {
                var filter = new PerformanceTargetFilter();
                if (!string.IsNullOrEmpty(employeeNo))
                {
                    filter.EmployeeNo = employeeNo;
                }

                var targets = await GetPerformanceTargetsAsync(filter);

                var summary = new PerformanceTargetSummary
                {
                    TotalTargets = targets.Count,
                    DraftTargets = targets.Count(t => t.Status == AppraisalStatus.DRAFT),
                    PendingApproval = targets.Count(t => t.Status == AppraisalStatus.PENDING_APPROVAL),
                    ApprovedTargets = targets.Count(t => t.Status == AppraisalStatus.APPROVED),
                    RejectedTargets = targets.Count(t => t.Status == AppraisalStatus.REJECTED)
                };

                // If employeeNo is provided, calculate additional stats
                if (!string.IsNullOrEmpty(employeeNo))
                {
                    var myTargets = targets.Where(t => t.EmployeeNo == employeeNo).ToList();
                    summary.MyDraftTargets = myTargets.Count(t => t.Status == AppraisalStatus.DRAFT);
                    summary.MyPendingApproval = myTargets.Count(t => t.Status == AppraisalStatus.PENDING_APPROVAL);
                }

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calculating target summary");
                return new PerformanceTargetSummary();
            }
        }

        #endregion

        #region Export Operations

        public async Task<byte[]> ExportTargetsToExcelAsync(PerformanceTargetFilter filter)
        {
            try
            {
                var targets = await GetPerformanceTargetsAsync(filter);

                using var package = new OfficeOpenXml.ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Performance Targets");

                // Add headers
                worksheet.Cells[1, 1].Value = "Objective No";
                worksheet.Cells[1, 2].Value = "Appraisee Name";
                worksheet.Cells[1, 3].Value = "Appraisee No";
                worksheet.Cells[1, 4].Value = "Appraiser Name";
                worksheet.Cells[1, 5].Value = "Department";
                worksheet.Cells[1, 6].Value = "Appraisal Category";
                worksheet.Cells[1, 7].Value = "Appraisal Period";
                worksheet.Cells[1, 8].Value = "Status";
                worksheet.Cells[1, 9].Value = "Created Date";
                worksheet.Cells[1, 10].Value = "Submitted Date";
                worksheet.Cells[1, 11].Value = "Approved Date";

                // Style headers
                using (var range = worksheet.Cells[1, 1, 1, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                // Add data
                int row = 2;
                foreach (var target in targets)
                {
                    worksheet.Cells[row, 1].Value = target.ObjectiveNo;
                    worksheet.Cells[row, 2].Value = target.AppraiseeName;
                    worksheet.Cells[row, 3].Value = target.EmployeeNo;
                    worksheet.Cells[row, 4].Value = target.AppraiserName;
                    worksheet.Cells[row, 5].Value = target.DepartmentName;
                    worksheet.Cells[row, 6].Value = target.AppraisalCategory;
                    worksheet.Cells[row, 7].Value = target.AppraisalPeriod;
                    worksheet.Cells[row, 8].Value = target.Status;
                    worksheet.Cells[row, 9].Value = target.CreatedDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 10].Value = target.SubmittedDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 11].Value = target.ApprovedDate?.ToString("yyyy-MM-dd");
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return await Task.FromResult(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error exporting targets to Excel");
                return Array.Empty<byte>();
            }
        }

        public async Task<byte[]> ExportTargetToPdfAsync(string objectiveNo)
        {
            // This would typically use a PDF library like QuestPDF or iTextSharp
            // For now, return empty array - implement based on your PDF library choice
            return await Task.FromResult(Array.Empty<byte>());
        }

        Task<string> IPerformanceTargetService.GenerateObjectiveNo()
        {
            return GenerateObjectiveNo();
        }

        public Task<Employee?> GetEmployeeAsync(string employeeNo)
        {
            throw new NotImplementedException();
        }

        public Task<List<Employee>> GetEmployeesByDepartmentAsync(string departmentCode)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Helper Classes

        private class ODataResponse<T>
        {
            [JsonProperty("@odata.context")]
            public string? Context { get; set; }

            [JsonProperty("value")]
            public List<T>? Value { get; set; }
        }

        private class BCErrorResponse
        {
            public BCError? Error { get; set; }
        }

        private class BCError
        {
            public string? Code { get; set; }
            public string? Message { get; set; }
        }

        #endregion
    }
}
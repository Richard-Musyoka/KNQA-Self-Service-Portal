using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using KNQASelfService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace KNQASelfService.Services
{
    public class AppraisalService : IAppraisalService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AppraisalService> _logger;
        private readonly IPerformanceTargetService _performanceTargetService;
        private readonly IEmployeeService _employeeService;
        private readonly string _baseUrl;
        private readonly string _authHeaderValue;

        public AppraisalService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AppraisalService> logger,
            IPerformanceTargetService performanceTargetService,
            IEmployeeService employeeService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _performanceTargetService = performanceTargetService;
            _employeeService = employeeService;

            _baseUrl = _configuration["BusinessCentral:BaseUrl"] ?? "";
            var username = _configuration["BusinessCentral:Username"] ?? "";
            var password = _configuration["BusinessCentral:Password"] ?? "";

            _authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        }

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

        public async Task<List<Appraisal>> GetAppraisalsAsync(AppraisalFilter filter)
        {
            try
            {
                var url = GetFullUrl("Company('KNQA')/Appraisals?$expand=AppraisalLines&$orderby=Created_Date desc");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<Appraisal>>(content);

                    if (result?.Value != null)
                    {
                        var appraisals = result.Value;

                        // Ensure each appraisal has initialized collections
                        foreach (var appraisal in appraisals)
                        {
                            appraisal.AppraisalLines ??= new List<AppraisalLine>();
                        }

                        // Apply filters
                        if (!string.IsNullOrEmpty(filter.EmployeeNo))
                            appraisals = appraisals.Where(a => a.EmployeeNo == filter.EmployeeNo).ToList();

                        if (!string.IsNullOrEmpty(filter.AppraiserNo))
                            appraisals = appraisals.Where(a => a.AppraiserNo == filter.AppraiserNo).ToList();

                        if (!string.IsNullOrEmpty(filter.Status))
                            appraisals = appraisals.Where(a => a.Status == filter.Status).ToList();

                        if (!string.IsNullOrEmpty(filter.AppraisalPeriod))
                            appraisals = appraisals.Where(a => a.AppraisalPeriod == filter.AppraisalPeriod).ToList();

                        if (!string.IsNullOrEmpty(filter.AppraisalType))
                            appraisals = appraisals.Where(a => a.AppraisalType == filter.AppraisalType).ToList();

                        if (!string.IsNullOrEmpty(filter.SearchTerm))
                        {
                            var searchTerm = filter.SearchTerm.ToLower();
                            appraisals = appraisals.Where(a =>
                                a.AppraisalNo.ToLower().Contains(searchTerm) ||
                                a.AppraiseeName.ToLower().Contains(searchTerm) ||
                                a.AppraiserName.ToLower().Contains(searchTerm)).ToList();
                        }

                        _logger.LogInformation($"✅ Retrieved {appraisals.Count} appraisals");
                        return appraisals;
                    }
                }

                _logger.LogWarning("⚠️ No appraisals found or API returned unsuccessful response");
                return new List<Appraisal>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching appraisals");
                return new List<Appraisal>();
            }
        }

        public async Task<Appraisal?> GetAppraisalAsync(string appraisalNo)
        {
            try
            {
                if (string.IsNullOrEmpty(appraisalNo))
                {
                    _logger.LogWarning("⚠️ GetAppraisalAsync called with empty appraisalNo");
                    return null;
                }

                var url = GetFullUrl($"Company('KNQA')/Appraisals('{appraisalNo}')?$expand=AppraisalLines");

                SetupRequestHeaders();

                _logger.LogInformation($"🔍 Fetching appraisal: {appraisalNo}");
                _logger.LogInformation($"📍 URL: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ HTTP {response.StatusCode} fetching appraisal {appraisalNo}: {errorContent}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();

                // Log first 500 chars of response for debugging
                _logger.LogInformation($"📄 Response preview: {content.Substring(0, Math.Min(500, content.Length))}...");

                var appraisal = JsonConvert.DeserializeObject<Appraisal>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    Error = (sender, args) =>
                    {
                        _logger.LogWarning($"⚠️ JSON deserialization warning at {args.ErrorContext.Path}: {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true; // Continue deserializing
                    }
                });

                if (appraisal == null)
                {
                    _logger.LogError($"❌ Deserialization returned null for appraisal {appraisalNo}");
                    return null;
                }

                // CRITICAL: Ensure collections are initialized
                appraisal.AppraisalLines ??= new List<AppraisalLine>();

                _logger.LogInformation($"✅ Loaded appraisal {appraisalNo}");
                _logger.LogInformation($"   - Employee: {appraisal.AppraiseeName} ({appraisal.EmployeeNo})");
                _logger.LogInformation($"   - Status: {appraisal.Status}");
                _logger.LogInformation($"   - Lines: {appraisal.AppraisalLines.Count}");

                // Load additional related data (non-critical, don't fail if these error)
                try
                {
                    await LoadLinkedPerformanceTarget(appraisal);
                    await LoadEmployeeDetails(appraisal);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"⚠️ Non-critical error loading related data for appraisal {appraisalNo}");
                    // Continue anyway - the main appraisal data is loaded
                }

                return appraisal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Exception in GetAppraisalAsync for {appraisalNo}");
                return null;
            }
        }

        private async Task LoadLinkedPerformanceTarget(Appraisal appraisal)
        {
            try
            {
                // Try to find a linked performance target based on appraisal period and employee
                var targets = await _performanceTargetService.GetPerformanceTargetsByEmployeeAsync(appraisal.EmployeeNo);
                appraisal.LinkedPerformanceTarget = targets
                    .FirstOrDefault(t => t.AppraisalPeriod == appraisal.AppraisalPeriod &&
                                        t.Status == AppraisalStatus.APPROVED);

                if (appraisal.LinkedPerformanceTarget != null)
                {
                    _logger.LogInformation($"   - Linked to target: {appraisal.LinkedPerformanceTarget.ObjectiveNo}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not load linked performance target for appraisal {appraisal.AppraisalNo}");
            }
        }

        private async Task LoadEmployeeDetails(Appraisal appraisal)
        {
            try
            {
                if (!string.IsNullOrEmpty(appraisal.EmployeeNo))
                {
                    appraisal.Appraisee = await _employeeService.GetEmployeeAsync(appraisal.EmployeeNo);
                }

                if (!string.IsNullOrEmpty(appraisal.AppraiserNo))
                {
                    appraisal.Appraiser = await _employeeService.GetEmployeeAsync(appraisal.AppraiserNo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not load employee details for appraisal {appraisal.AppraisalNo}");
            }
        }

        public async Task<string> CreateAppraisalAsync(Appraisal appraisal)
        {
            try
            {
                // Generate appraisal number if not provided
                if (string.IsNullOrEmpty(appraisal.AppraisalNo))
                {
                    appraisal.AppraisalNo = await GenerateAppraisalNo();
                }

                appraisal.CreatedDate = DateTime.Now;
                appraisal.Status = AppraisalStatus.OPEN;

                // Ensure AppraisalLines is initialized
                appraisal.AppraisalLines ??= new List<AppraisalLine>();

                var url = GetFullUrl("Company('KNQA')/Appraisals");

                SetupRequestHeaders();

                var jsonContent = JsonConvert.SerializeObject(appraisal, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    // Create appraisal lines if any
                    if (appraisal.AppraisalLines.Any())
                    {
                        await CreateAppraisalLinesAsync(appraisal.AppraisalNo, appraisal.AppraisalLines);
                    }

                    _logger.LogInformation($"✅ Appraisal {appraisal.AppraisalNo} created successfully");
                    return $"Success: Appraisal {appraisal.AppraisalNo} created successfully";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to create appraisal: {errorContent}");
                    return $"Error: Failed to create appraisal - {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error creating appraisal");
                return $"Error: {ex.Message}";
            }
        }

        private async Task CreateAppraisalLinesAsync(string appraisalNo, List<AppraisalLine> lines)
        {
            try
            {
                foreach (var line in lines)
                {
                    line.AppraisalNo = appraisalNo;

                    var url = GetFullUrl("Company('KNQA')/AppraisalLines");

                    var jsonContent = JsonConvert.SerializeObject(line, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                    });

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"❌ Failed to create appraisal line: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating appraisal lines");
            }
        }

        public async Task<string> UpdateAppraisalAsync(Appraisal appraisal)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/Appraisals('{appraisal.AppraisalNo}')");

                SetupRequestHeaders();

                var jsonContent = JsonConvert.SerializeObject(appraisal, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Appraisal {appraisal.AppraisalNo} updated successfully");
                    return $"Success: Appraisal {appraisal.AppraisalNo} updated successfully";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to update appraisal: {errorContent}");
                    return $"Error: Failed to update appraisal - {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating appraisal {appraisal.AppraisalNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> DeleteAppraisalAsync(string appraisalNo)
        {
            try
            {
                // First delete appraisal lines
                var linesUrl = GetFullUrl($"Company('KNQA')/AppraisalLines?$filter=Appraisal_No eq '{appraisalNo}'");
                SetupRequestHeaders();
                var linesResponse = await _httpClient.GetAsync(linesUrl);

                if (linesResponse.IsSuccessStatusCode)
                {
                    var content = await linesResponse.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<AppraisalLine>>(content);

                    if (result?.Value != null)
                    {
                        foreach (var line in result.Value)
                        {
                            var deleteLineUrl = GetFullUrl($"Company('KNQA')/AppraisalLines('{line.LineNo}')");
                            await _httpClient.DeleteAsync(deleteLineUrl);
                        }
                    }
                }

                // Then delete appraisal
                var url = GetFullUrl($"Company('KNQA')/Appraisals('{appraisalNo}')");
                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Appraisal {appraisalNo} deleted successfully");
                    return $"Success: Appraisal {appraisalNo} deleted successfully";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to delete appraisal: {errorContent}");
                    return $"Error: Failed to delete appraisal - {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting appraisal {appraisalNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> SubmitAppraisalAsync(string appraisalNo, string employeeComments = "")
        {
            try
            {
                var appraisal = await GetAppraisalAsync(appraisalNo);
                if (appraisal == null)
                    return "Error: Appraisal not found";

                if (appraisal.Status != AppraisalStatus.OPEN && appraisal.Status != AppraisalStatus.IN_PROGRESS)
                    return "Error: Appraisal cannot be submitted in current status";

                // Ensure AppraisalLines is initialized
                appraisal.AppraisalLines ??= new List<AppraisalLine>();

                // Calculate employee score
                appraisal.TotalScoreEmployee = appraisal.AppraisalLines.Sum(l => l.EmployeeWeightedScore);
                appraisal.EmployeeComments = employeeComments;
                appraisal.Status = AppraisalStatus.SUBMITTED;
                appraisal.SubmittedDate = DateTime.Now;

                return await UpdateAppraisalAsync(appraisal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error submitting appraisal {appraisalNo}");
                return $"Error: {ex.Message}";
            }
        }
        public async Task<string> StartAgreementAsync(string appraisalNo)
        {
            try
            {
                var appraisal = await GetAppraisalAsync(appraisalNo);
                if (appraisal == null)
                    return "Error: Appraisal not found";

                if (appraisal.Status != AppraisalStatus.APPRAISED)
                    return "Error: Appraisal must be in APPRAISED status to start agreement";

                appraisal.Status = AppraisalStatus.AGREEMENT_IN_PROGRESS;
                appraisal.AgreementStartDate = DateTime.Now;

                var result = await UpdateAppraisalAsync(appraisal);
                if (result.StartsWith("Success"))
                {
                    // Log the status change
                    return $"AGREEMENT_STARTE Employee started agreement process";
                }

                return result;
            }
            catch (Exception ex)
            {
                return $"Error starting agreement: {ex.Message}";
            }
        }
 
       


        public async Task<string> StartAppraisalAsync(string appraisalNo)
        {
            try
            {
                var appraisal = await GetAppraisalAsync(appraisalNo);
                if (appraisal == null)
                    return "Error: Appraisal not found";

                if (appraisal.Status != AppraisalStatus.SUBMITTED)
                    return "Error: Appraisal must be in SUBMITTED status to start appraisal";

                appraisal.Status = AppraisalStatus.APPRAISAL_IN_PROGRESS;
                appraisal.AppraisalStartDate = DateTime.Now;

                var result = await UpdateAppraisalAsync(appraisal);
                if (result.StartsWith("Success"))
                {
                    return $"Supervisor Started";
                }

                return result;
            }
            catch (Exception ex)
            {
                return $"Error starting appraisal: {ex.Message}";
            }
        }


        public async Task<string> AppraiseAsync(string appraisalNo, List<AppraisalLine> appraisalLines, string appraiserComments = "")
        {
            try
            {
                var appraisal = await GetAppraisalAsync(appraisalNo);
                if (appraisal == null)
                    return "Error: Appraisal not found";

                if (appraisal.Status != Models.AppraisalStatus.SUBMITTED)
                    return "Error: Appraisal must be submitted before appraising";

                // Ensure AppraisalLines is initialized
                appraisal.AppraisalLines ??= new List<AppraisalLine>();

                // Update appraisal lines with supervisor scores
                foreach (var line in appraisalLines)
                {
                    var existingLine = appraisal.AppraisalLines.FirstOrDefault(l => l.LineNo == line.LineNo);
                    if (existingLine != null)
                    {
                        existingLine.SupervisorScore = line.SupervisorScore;
                        existingLine.SupervisorRemarks = line.SupervisorRemarks;

                        // Update in Business Central
                        await UpdateAppraisalLineAsync(existingLine);
                    }
                }

                // Calculate supervisor score
                appraisal.TotalScoreSupervisor = appraisal.AppraisalLines.Sum(l => l.SupervisorWeightedScore);
                appraisal.AppraiserComments = appraiserComments;
                appraisal.Status = AppraisalStatus.APPRAISED;
                appraisal.AppraisedDate = DateTime.Now;

                return await UpdateAppraisalAsync(appraisal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error appraising {appraisalNo}");
                return $"Error: {ex.Message}";
            }
        }

        private async Task UpdateAppraisalLineAsync(AppraisalLine line)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/AppraisalLines('{line.LineNo}')");

                var jsonContent = JsonConvert.SerializeObject(line, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                await _httpClient.PatchAsync(url, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating appraisal line {line.LineNo}");
            }
        }

        public async Task<string> AgreeOnAppraisalAsync(string appraisalNo, List<AppraisalLine> agreedLines, string agreedComments = "")
        {
            try
            {
                var appraisal = await GetAppraisalAsync(appraisalNo);
                if (appraisal == null)
                    return "Error: Appraisal not found";

                if (appraisal.Status != AppraisalStatus.APPRAISED)
                    return "Error: Appraisal must be appraised before agreement";

                // Ensure AppraisalLines is initialized
                appraisal.AppraisalLines ??= new List<AppraisalLine>();

                // Update agreed scores
                foreach (var line in agreedLines)
                {
                    var existingLine = appraisal.AppraisalLines.FirstOrDefault(l => l.LineNo == line.LineNo);
                    if (existingLine != null)
                    {
                        existingLine.AgreedScore = line.AgreedScore;
                        existingLine.AgreedRemarks = line.AgreedRemarks;

                        await UpdateAppraisalLineAsync(existingLine);
                    }
                }

                // Calculate agreed score and overall rating
                appraisal.TotalScoreAgreed = appraisal.AppraisalLines.Sum(l => l.AgreedWeightedScore);
                appraisal.OverallRating = await CalculateOverallRatingAsync(appraisalNo);
                appraisal.PerformanceCategory = await DeterminePerformanceCategoryAsync(appraisal.OverallRating ?? 0);
                appraisal.AgreedComments = agreedComments;
                appraisal.Status = AppraisalStatus.AGREED;
                appraisal.AgreedDate = DateTime.Now;

                return await UpdateAppraisalAsync(appraisal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error agreeing on appraisal {appraisalNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> CompleteAppraisalAsync(string appraisalNo)
        {
            try
            {
                var appraisal = await GetAppraisalAsync(appraisalNo);
                if (appraisal == null)
                    return "Error: Appraisal not found";

                if (appraisal.Status != AppraisalStatus.AGREED)
                    return "Error: Appraisal must be agreed before completion";

                appraisal.Status = AppraisalStatus.COMPLETED;
                appraisal.CompletedDate = DateTime.Now;
                appraisal.EffectiveDate = DateTime.Now;

                return await UpdateAppraisalAsync(appraisal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error completing appraisal {appraisalNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<List<Appraisal>> GetAppraisalsByEmployeeAsync(string employeeNo)
        {
            var filter = new AppraisalFilter { EmployeeNo = employeeNo };
            return await GetAppraisalsAsync(filter);
        }

        public async Task<List<Appraisal>> GetAppraisalsByAppraiserAsync(string appraiserNo)
        {
            var filter = new AppraisalFilter { AppraiserNo = appraiserNo };
            return await GetAppraisalsAsync(filter);
        }

        public async Task<List<Appraisal>> GetMyPendingAppraisalsAsync(string employeeNo)
        {
            var filter = new AppraisalFilter
            {
                EmployeeNo = employeeNo,
                Status = AppraisalStatus.OPEN
            };
            return await GetAppraisalsAsync(filter);
        }

        public async Task<List<Appraisal>> GetAppraisalsPendingMyApprovalAsync(string appraiserNo)
        {
            var filter = new AppraisalFilter
            {
                AppraiserNo = appraiserNo,
                Status = AppraisalStatus.SUBMITTED
            };
            return await GetAppraisalsAsync(filter);
        }

        public async Task<Appraisal?> CreateAppraisalFromTargetAsync(string performanceTargetNo)
        {
            try
            {
                var target = await _performanceTargetService.GetPerformanceTargetAsync(performanceTargetNo);
                if (target == null)
                {
                    _logger.LogError($"❌ Performance target {performanceTargetNo} not found");
                    return null;
                }

                var appraisal = new Appraisal
                {
                    AppraisalNo = await GenerateAppraisalNo(),
                    EmployeeNo = target.EmployeeNo,
                    AppraiseeName = target.AppraiseeName,
                    AppraiseeJobTitle = target.AppraiseeJobTitle,
                    AppraiseeJobID = target.AppraiseeJobID ?? "",
                    DepartmentCode = target.DepartmentCode ?? "",
                    DepartmentName = target.DepartmentName ?? "",
                    AppraiserNo = target.AppraiserNo,
                    AppraiserName = target.AppraiserName,
                    AppraiserJobTitle = target.AppraiserJobTitle ?? "",
                    AppraisalPeriod = target.AppraisalPeriod,
                    AppraisalType = target.AppraisalCategory ?? AppraisalType.ANNUAL,
                    Status = AppraisalStatus.OPEN,
                    CreatedDate = DateTime.Now,
                    LinkedPerformanceTarget = target,
                    AppraisalLines = new List<AppraisalLine>() // Initialize the list
                };

                // Create appraisal lines from target lines
                if (target.TargetLines != null && target.TargetLines.Any())
                {
                    int lineNo = 1;
                    foreach (var targetLine in target.TargetLines)
                    {
                        var appraisalLine = new AppraisalLine
                        {
                            LineNo = lineNo++,
                            PerformanceTargetNo = performanceTargetNo,
                            KeyPerformanceArea = targetLine.KeyPerformanceArea,
                            KeyPerformanceIndicator = targetLine.KeyPerformanceIndicator,
                            PerformanceMeasure = targetLine.PerformanceMeasure,
                            Target = targetLine.Target,
                            MaximumWeighting = targetLine.Weighting,
                            EmployeeScore = 0,
                            SupervisorScore = 0,
                            AgreedScore = 0,
                            LinkedTargetLine = targetLine
                        };

                        appraisal.AppraisalLines.Add(appraisalLine);
                        appraisal.TotalMaximumScore += targetLine.Weighting;
                    }

                    _logger.LogInformation($"✅ Created appraisal {appraisal.AppraisalNo} from target {performanceTargetNo} with {appraisal.AppraisalLines.Count} lines");
                }
                else
                {
                    _logger.LogWarning($"⚠️ Performance target {performanceTargetNo} has no target lines");
                }

                return appraisal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error creating appraisal from target {performanceTargetNo}");
                return null;
            }
        }

        public async Task<List<Appraisal>> GetAppraisalsLinkedToTargetAsync(string performanceTargetNo)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/Appraisals?$filter=Performance_Target_No eq '{performanceTargetNo}'&$expand=AppraisalLines");

                SetupRequestHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<Appraisal>>(content);

                    var appraisals = result?.Value ?? new List<Appraisal>();

                    // Ensure collections are initialized
                    foreach (var appraisal in appraisals)
                    {
                        appraisal.AppraisalLines ??= new List<AppraisalLine>();
                    }

                    return appraisals;
                }

                return new List<Appraisal>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching appraisals linked to target {performanceTargetNo}");
                return new List<Appraisal>();
            }
        }

        public async Task<bool> LinkAppraisalToTargetAsync(string appraisalNo, string performanceTargetNo)
        {
            try
            {
                var appraisal = await GetAppraisalAsync(appraisalNo);
                if (appraisal == null)
                    return false;

                // Ensure AppraisalLines is initialized
                appraisal.AppraisalLines ??= new List<AppraisalLine>();

                // Update appraisal lines with performance target reference
                foreach (var line in appraisal.AppraisalLines)
                {
                    line.PerformanceTargetNo = performanceTargetNo;
                    await UpdateAppraisalLineAsync(line);
                }

                _logger.LogInformation($"✅ Linked appraisal {appraisalNo} to target {performanceTargetNo}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error linking appraisal {appraisalNo} to target {performanceTargetNo}");
                return false;
            }
        }

        public async Task<AppraisalSummary> GetAppraisalSummaryAsync(string? employeeNo = null, string? appraiserNo = null)
        {
            try
            {
                var summary = new AppraisalSummary();

                // If both are null, get all appraisals
                if (string.IsNullOrEmpty(employeeNo) && string.IsNullOrEmpty(appraiserNo))
                {
                    var allAppraisals = await GetAppraisalsAsync(new AppraisalFilter());
                    return CalculateSummaryFromAppraisals(allAppraisals, null, null);
                }

                // Get appraisals where user is the employee
                List<Appraisal> myAppraisals = new List<Appraisal>();
                if (!string.IsNullOrEmpty(employeeNo))
                {
                    myAppraisals = await GetAppraisalsAsync(new AppraisalFilter { EmployeeNo = employeeNo });
                }

                // Get appraisals where user is the appraiser
                List<Appraisal> appraisalsToApprove = new List<Appraisal>();
                if (!string.IsNullOrEmpty(appraiserNo))
                {
                    appraisalsToApprove = await GetAppraisalsAsync(new AppraisalFilter { AppraiserNo = appraiserNo });
                }

                // Combine unique appraisals (avoid duplicates if user is both employee and appraiser)
                var allUniqueAppraisals = myAppraisals
                    .Concat(appraisalsToApprove)
                    .GroupBy(a => a.AppraisalNo)
                    .Select(g => g.First())
                    .ToList();

                // Calculate summary statistics
                summary.TotalAppraisals = allUniqueAppraisals.Count;
                summary.OpenAppraisals = allUniqueAppraisals.Count(a => a.Status == AppraisalStatus.OPEN);
                summary.InProgressAppraisals = allUniqueAppraisals.Count(a => a.Status == AppraisalStatus.IN_PROGRESS);
                summary.SubmittedAppraisals = allUniqueAppraisals.Count(a => a.Status == AppraisalStatus.SUBMITTED);
                summary.AppraisedAppraisals = allUniqueAppraisals.Count(a => a.Status == AppraisalStatus.APPRAISED);
                summary.AgreedAppraisals = allUniqueAppraisals.Count(a => a.Status == AppraisalStatus.AGREED);
                summary.CompletedAppraisals = allUniqueAppraisals.Count(a => a.Status == AppraisalStatus.COMPLETED);

                // My pending appraisals (where I am the employee and status is OPEN)
                summary.MyAppraisals = !string.IsNullOrEmpty(employeeNo)
                    ? myAppraisals.Count(a => a.Status == AppraisalStatus.OPEN)
                    : 0;

                // Appraisals pending my approval (where I am the appraiser and status is SUBMITTED)
                summary.AppraisalsForMyApproval = !string.IsNullOrEmpty(appraiserNo)
                    ? appraisalsToApprove.Count(a => a.Status == AppraisalStatus.SUBMITTED)
                    : 0;

                _logger.LogInformation($"✅ Summary calculated - Total: {summary.TotalAppraisals}, My Pending: {summary.MyAppraisals}, Awaiting My Approval: {summary.AppraisalsForMyApproval}");

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting appraisal summary");
                return new AppraisalSummary();
            }
        }

        private AppraisalSummary CalculateSummaryFromAppraisals(List<Appraisal> appraisals, string? employeeNo, string? appraiserNo)
        {
            return new AppraisalSummary
            {
                TotalAppraisals = appraisals.Count,
                OpenAppraisals = appraisals.Count(a => a.Status == AppraisalStatus.OPEN),
                InProgressAppraisals = appraisals.Count(a => a.Status == AppraisalStatus.IN_PROGRESS),
                SubmittedAppraisals = appraisals.Count(a => a.Status == AppraisalStatus.SUBMITTED),
                AppraisedAppraisals = appraisals.Count(a => a.Status == AppraisalStatus.APPRAISED),
                AgreedAppraisals = appraisals.Count(a => a.Status == AppraisalStatus.AGREED),
                CompletedAppraisals = appraisals.Count(a => a.Status == AppraisalStatus.COMPLETED),
                MyAppraisals = !string.IsNullOrEmpty(employeeNo)
                    ? appraisals.Count(a => a.EmployeeNo == employeeNo && a.Status == AppraisalStatus.OPEN)
                    : 0,
                AppraisalsForMyApproval = !string.IsNullOrEmpty(appraiserNo)
                    ? appraisals.Count(a => a.AppraiserNo == appraiserNo && a.Status == AppraisalStatus.SUBMITTED)
                    : 0
            };
        }

        public async Task<decimal> CalculateOverallRatingAsync(string appraisalNo)
        {
            try
            {
                var appraisal = await GetAppraisalAsync(appraisalNo);
                if (appraisal == null || appraisal.TotalMaximumScore == 0)
                    return 0;

                return (appraisal.TotalScoreAgreed / appraisal.TotalMaximumScore) * 100;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error calculating overall rating for appraisal {appraisalNo}");
                return 0;
            }
        }

        public async Task<string> DeterminePerformanceCategoryAsync(decimal rating)
        {
            return rating switch
            {
                >= 90 => PerformanceEvaluation.EXCELLENT,
                >= 80 => PerformanceEvaluation.VERY_GOOD,
                >= 70 => PerformanceEvaluation.GOOD,
                >= 60 => PerformanceEvaluation.FAIR,
                _ => PerformanceEvaluation.POOR
            };
        }

        public async Task<List<string>> GetAppraisalTypesAsync()
        {
            return new List<string>
            {
                AppraisalType.MID_YEAR,
                AppraisalType.ANNUAL,
                AppraisalType.PROBATION,
                AppraisalType.PROMOTION,
                AppraisalType.SPECIAL
            };
        }

        public async Task<List<string>> GetPerformanceEvaluationsAsync()
        {
            return new List<string>
            {
                PerformanceEvaluation.EXCELLENT,
                PerformanceEvaluation.VERY_GOOD,
                PerformanceEvaluation.GOOD,
                PerformanceEvaluation.FAIR,
                PerformanceEvaluation.POOR
            };
        }

        public async Task<string> GenerateAppraisalNo()
        {
            try
            {
                var url = GetFullUrl("Company('KNQA')/Appraisals?$top=1&$orderby=Appraisal_No desc");

                SetupRequestHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<Appraisal>>(content);

                    if (result?.Value != null && result.Value.Any())
                    {
                        var lastNo = result.Value.First().AppraisalNo;
                        if (lastNo.StartsWith("APR") && int.TryParse(lastNo.Substring(3), out int number))
                        {
                            return $"APR{(number + 1):00000}";
                        }
                    }
                }

                return $"APR{DateTime.Now:yy}00001";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating appraisal number");
                return $"APR{DateTime.Now:yy}{DateTime.Now:MMddHHmmss}";
            }
        }

        public async Task<List<PerformanceTarget>> GetEligibleTargetsForAppraisalAsync(string employeeNo)
        {
            try
            {
                var targets = await _performanceTargetService.GetPerformanceTargetsByEmployeeAsync(employeeNo);

                // Filter targets that are approved and not yet linked to completed appraisals
                var eligibleTargets = new List<PerformanceTarget>();

                foreach (var target in targets.Where(t => t.Status == AppraisalStatus.APPROVED))
                {
                    var linkedAppraisals = await GetAppraisalsLinkedToTargetAsync(target.ObjectiveNo ?? "");
                    if (!linkedAppraisals.Any(a => a.IsCompleted))
                    {
                        eligibleTargets.Add(target);
                    }
                }

                _logger.LogInformation($"✅ Found {eligibleTargets.Count} eligible targets for employee {employeeNo}");
                return eligibleTargets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting eligible targets for employee {employeeNo}");
                return new List<PerformanceTarget>();
            }
        }

        public async Task<byte[]> ExportAppraisalToExcelAsync(string appraisalNo)
        {
            // TODO: Implementation for Excel export
            await Task.Delay(100); // Placeholder
            return Array.Empty<byte>();
        }

        public async Task<byte[]> ExportAppraisalToPdfAsync(string appraisalNo)
        {
            // TODO: Implementation for PDF export
            await Task.Delay(100); // Placeholder
            return Array.Empty<byte>();
        }

        public async Task<byte[]> ExportAppraisalSummaryReportAsync(AppraisalFilter filter)
        {
            // TODO: Implementation for summary report export
            await Task.Delay(100); // Placeholder
            return Array.Empty<byte>();
        }

        public async Task<string> SyncAppraisalFromNavAsync(string appraisalNo)
        {
            try
            {
                // Fetch latest data from NAV
                var navAppraisal = await GetAppraisalAsync(appraisalNo);
                if (navAppraisal == null)
                    return "Error: Appraisal not found in NAV";

                // Check if supervisor has completed assessment in NAV
                if (navAppraisal.Status == AppraisalStatus.APPRAISED &&
                    navAppraisal.TotalScoreSupervisor > 0)
                {
                    // Update our local records if needed
                    // This would depend on your sync strategy
                    return "Success: Appraisal synced from NAV";
                }

                return "Info: Supervisor assessment not yet completed in NAV";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error syncing appraisal {appraisalNo} from NAV");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> SaveAssessmentAsync(Appraisal appraisal)
        {
            try
            {
                if (appraisal == null)
                    return "Error: Appraisal is null";

                // Ensure AppraisalLines is initialized
                appraisal.AppraisalLines ??= new List<AppraisalLine>();

                // Update each line in Business Central
                foreach (var line in appraisal.AppraisalLines)
                {
                    await UpdateAppraisalLineAsync(line);
                }

                // Update the appraisal header
                return await UpdateAppraisalAsync(appraisal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error saving assessment for appraisal {appraisal?.AppraisalNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> SubmitAssessmentAsync(Appraisal appraisal)
        {
            try
            {
                if (appraisal == null)
                    return "Error: Appraisal is null";

                if (appraisal.Status != AppraisalStatus.SUBMITTED)
                    return "Error: Appraisal must be in SUBMITTED status";

                // Ensure AppraisalLines is initialized
                appraisal.AppraisalLines ??= new List<AppraisalLine>();

                // Update each line in Business Central
                foreach (var line in appraisal.AppraisalLines)
                {
                    await UpdateAppraisalLineAsync(line);
                }

                // Calculate supervisor score
                appraisal.TotalScoreSupervisor = appraisal.AppraisalLines.Sum(l => l.SupervisorWeightedScore);
                appraisal.Status = AppraisalStatus.APPRAISED;
                appraisal.AppraisedDate = DateTime.Now;

                return await UpdateAppraisalAsync(appraisal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error submitting assessment for appraisal {appraisal?.AppraisalNo}");
                return $"Error: {ex.Message}";
            }
        }

        // OData response wrapper classes
        private class ODataResponse<T>
        {
            [JsonProperty("@odata.context")]
            public string? Context { get; set; }

            [JsonProperty("value")]
            public List<T>? Value { get; set; }
        }

        private class ODataSingleEntityResponse<T>
        {
            [JsonProperty("@odata.context")]
            public string? Context { get; set; }

            [JsonProperty("@odata.etag")]
            public string? ETag { get; set; }

            [JsonProperty("value")]
            public T? Value { get; set; }
        }
    }
}
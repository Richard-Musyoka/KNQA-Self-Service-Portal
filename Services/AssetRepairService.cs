using KNQASelfService.Models;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace KNQASelfService.Services
{
    public class AssetRepairService : IAssetRepairService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AssetRepairService> _logger;
        private readonly IFixedAssetService _fixedAssetService;
        private readonly string _baseUrl;
        private readonly string _authHeaderValue;

        public AssetRepairService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AssetRepairService> logger,
            IFixedAssetService fixedAssetService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _fixedAssetService = fixedAssetService;

            _baseUrl = _configuration["BusinessCentral:BaseUrl"] ?? "";
            var username = _configuration["BusinessCentral:Username"] ?? "";
            var password = _configuration["BusinessCentral:Password"] ?? "";

            _authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
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

        private string BuildFilterQuery(AssetRepairFilter filter)
        {
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.Replace("'", "''");
                filters.Add($"(contains(MaintenanceRefNo, '{searchTerm}') or contains(AssetNo, '{searchTerm}') or contains(EmployeeName, '{searchTerm}') or contains(MaintenanceIssue, '{searchTerm}'))");
            }

            if (!string.IsNullOrWhiteSpace(filter.EmployeeNo))
            {
                filters.Add($"EmployeeNo eq '{filter.EmployeeNo.Replace("'", "''")}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.AssetNo))
            {
                filters.Add($"AssetNo eq '{filter.AssetNo.Replace("'", "''")}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.MaintenanceStatus))
            {
                filters.Add($"MaintenanceStatus eq '{filter.MaintenanceStatus.Replace("'", "''")}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.Department))
            {
                filters.Add($"Department eq '{filter.Department.Replace("'", "''")}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.RepairPriority))
            {
                filters.Add($"RepairPriority eq '{filter.RepairPriority.Replace("'", "''")}'");
            }

            if (filter.FromDate.HasValue)
            {
                filters.Add($"ReportedDate ge {filter.FromDate.Value:yyyy-MM-dd}");
            }

            if (filter.ToDate.HasValue)
            {
                filters.Add($"ReportedDate le {filter.ToDate.Value:yyyy-MM-dd}");
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

        private async Task<string> GenerateMaintenanceRefNo()
        {
            try
            {
                var currentYear = DateTime.Now.ToString("yyyy");
                var url = GetFullUrl($"Company('KNQA')/AssetRepairs?$filter=startswith(MaintenanceRefNo, 'MT') and contains(MaintenanceRefNo, '{currentYear}')&$top=1&$orderby=MaintenanceRefNo desc");

                SetupRequestHeaders();
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<AssetRepair>>(content);

                    if (result?.Value != null && result.Value.Any())
                    {
                        var lastRef = result.Value.First().MaintenanceRefNo;
                        if (lastRef != null && lastRef.StartsWith("MT"))
                        {
                            var parts = lastRef.Split('/');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
                            {
                                return $"MT{currentYear}/{lastNumber + 1:0000}";
                            }
                        }
                    }
                }

                // Default format if no existing records
                return $"MT{currentYear}/0001";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating maintenance reference number");
                return $"MT{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        #endregion

        #region CRUD Operations

        public async Task<List<AssetRepair>> GetAssetRepairsAsync(AssetRepairFilter filter)
        {
            try
            {
                var queryParams = BuildFilterQuery(filter);
                var url = GetFullUrl($"Company('KNQA')/AssetRepairs{queryParams}");

                SetupRequestHeaders();

                _logger.LogInformation($"🔍 Fetching asset repairs from: {url}");

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"✅ Asset Repairs API response received. Content length: {content.Length}");

                    try
                    {
                        var result = JsonConvert.DeserializeObject<ODataResponse<AssetRepair>>(content);
                        if (result?.Value != null)
                        {
                            _logger.LogInformation($"✅ Successfully loaded {result.Value.Count} asset repairs");
                            return result.Value;
                        }
                    }
                    catch (Newtonsoft.Json.JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "❌ Error deserializing asset repairs");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to fetch asset repairs. Status: {response.StatusCode}");
                    _logger.LogError($"❌ Error response: {errorContent}");
                }

                return new List<AssetRepair>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching asset repairs");
                return new List<AssetRepair>();
            }
        }

        public async Task<AssetRepair?> GetAssetRepairAsync(string maintenanceRefNo)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/AssetRepairs('{maintenanceRefNo}')");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var repair = JsonConvert.DeserializeObject<AssetRepair>(content);

                        if (response.Headers.TryGetValues("ETag", out var etagValues))
                        {
                            var etag = etagValues.FirstOrDefault();
                            if (!string.IsNullOrEmpty(etag))
                            {
                                etag = etag.Trim('"');
                                if (repair != null) repair.ODataEtag = etag;
                            }
                        }

                        return repair;
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
                _logger.LogError(ex, $"❌ Error fetching asset repair {maintenanceRefNo}");
                return null;
            }
        }

        public async Task<string> CreateAssetRepairAsync(AssetRepair repair)
        {
            try
            {
                // Generate reference number if not provided
                if (string.IsNullOrEmpty(repair.MaintenanceRefNo))
                {
                    repair.MaintenanceRefNo = await GenerateMaintenanceRefNo();
                }

                // Get asset details if AssetNo is provided
                if (!string.IsNullOrEmpty(repair.AssetNo))
                {
                    var asset = await _fixedAssetService.GetFixedAssetAsync(repair.AssetNo);
                    if (asset != null && string.IsNullOrEmpty(repair.AssetName))
                    {
                        repair.AssetName = asset.Description;
                    }
                }

                var url = GetFullUrl("Company('KNQA')/AssetRepairs");

                SetupRequestHeaders();

                var json = JsonConvert.SerializeObject(new
                {
                    MaintenanceRefNo = repair.MaintenanceRefNo,
                    EmployeeNo = repair.EmployeeNo,
                    EmployeeName = repair.EmployeeName,
                    JobTitle = repair.JobTitle,
                    SupervisorName = repair.SupervisorName,
                    Department = repair.Department,
                    MaintenanceStatus = repair.MaintenanceStatus,
                    MaintenanceDetails = repair.MaintenanceDetails,
                    AssetNo = repair.AssetNo,
                    AssetName = repair.AssetName,
                    MaintenanceIssue = repair.MaintenanceIssue,
                    ReportedDate = repair.ReportedDate?.ToString("yyyy-MM-dd"),
                    EstimatedCost = repair.EstimatedCost,
                    RepairPriority = repair.RepairPriority,
                    Remarks = repair.Remarks
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"📤 Creating asset repair: {repair.MaintenanceRefNo}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Asset repair {repair.MaintenanceRefNo} created successfully");
                    return $"Success: Repair request {repair.MaintenanceRefNo} created successfully";
                }
                else
                {
                    _logger.LogError($"❌ Failed to create repair. Status: {response.StatusCode}, Response: {responseContent}");
                    return ParseErrorResponse(responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error creating asset repair {repair.MaintenanceRefNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateAssetRepairAsync(AssetRepair repair)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/AssetRepairs('{repair.MaintenanceRefNo}')");

                SetupRequestHeaders();

                var json = JsonConvert.SerializeObject(new
                {
                    EmployeeNo = repair.EmployeeNo,
                    EmployeeName = repair.EmployeeName,
                    JobTitle = repair.JobTitle,
                    SupervisorName = repair.SupervisorName,
                    Department = repair.Department,
                    MaintenanceStatus = repair.MaintenanceStatus,
                    MaintenanceDetails = repair.MaintenanceDetails,
                    AssetNo = repair.AssetNo,
                    AssetName = repair.AssetName,
                    MaintenanceIssue = repair.MaintenanceIssue,
                    ReportedDate = repair.ReportedDate?.ToString("yyyy-MM-dd"),
                    CompletedDate = repair.CompletedDate?.ToString("yyyy-MM-dd"),
                    EstimatedCost = repair.EstimatedCost,
                    ActualCost = repair.ActualCost,
                    RepairPriority = repair.RepairPriority,
                    AssignedTechnician = repair.AssignedTechnician,
                    ResolutionDetails = repair.ResolutionDetails,
                    Remarks = repair.Remarks
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Add If-Match header for concurrency control
                if (!string.IsNullOrEmpty(repair.ODataEtag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", repair.ODataEtag);
                }

                _logger.LogInformation($"🔄 Updating asset repair: {repair.MaintenanceRefNo}");

                var response = await _httpClient.PatchAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Asset repair {repair.MaintenanceRefNo} updated successfully");
                    return $"Success: Repair request {repair.MaintenanceRefNo} updated successfully";
                }
                else
                {
                    _logger.LogError($"❌ Failed to update repair. Status: {response.StatusCode}, Response: {responseContent}");
                    return ParseErrorResponse(responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating asset repair {repair.MaintenanceRefNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> DeleteAssetRepairAsync(string maintenanceRefNo)
        {
            try
            {
                _logger.LogInformation($"🗑️ Deleting asset repair: {maintenanceRefNo}");

                var repair = await GetAssetRepairAsync(maintenanceRefNo);
                if (repair == null)
                {
                    return $"Error: Repair request {maintenanceRefNo} not found";
                }

                var url = GetFullUrl($"Company('KNQA')/AssetRepairs('{maintenanceRefNo}')");

                SetupRequestHeaders();

                if (!string.IsNullOrEmpty(repair.ODataEtag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", repair.ODataEtag);
                }

                var response = await _httpClient.DeleteAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Asset repair {maintenanceRefNo} deleted successfully");
                    return $"Success: Repair request {maintenanceRefNo} deleted successfully";
                }
                else
                {
                    _logger.LogError($"❌ Failed to delete repair. Status: {response.StatusCode}, Response: {responseContent}");
                    return ParseErrorResponse(responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting asset repair {maintenanceRefNo}");
                return $"Error: {ex.Message}";
            }
        }

        #endregion

        #region Employee-specific Operations

        public async Task<List<AssetRepair>> GetRepairsByEmployeeAsync(string employeeNo)
        {
            try
            {
                var filter = new AssetRepairFilter
                {
                    EmployeeNo = employeeNo,
                    OrderBy = "ReportedDate desc"
                };

                return await GetAssetRepairsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching repairs for employee {employeeNo}");
                return new List<AssetRepair>();
            }
        }

        public async Task<List<AssetRepair>> GetMyOpenRepairsAsync(string employeeNo)
        {
            try
            {
                var filter = new AssetRepairFilter
                {
                    EmployeeNo = employeeNo,
                    MaintenanceStatus = RepairStatus.OPEN,
                    OrderBy = "ReportedDate desc"
                };

                return await GetAssetRepairsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching open repairs for employee {employeeNo}");
                return new List<AssetRepair>();
            }
        }

        #endregion

        #region Asset-related Operations

        public async Task<List<AssetRepair>> GetRepairsByAssetAsync(string assetNo)
        {
            try
            {
                var filter = new AssetRepairFilter
                {
                    AssetNo = assetNo,
                    OrderBy = "ReportedDate desc"
                };

                return await GetAssetRepairsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching repairs for asset {assetNo}");
                return new List<AssetRepair>();
            }
        }

        #endregion

        #region Summary and Reporting

        public async Task<AssetRepairSummary> GetRepairSummaryAsync(string? employeeNo = null)
        {
            try
            {
                var filter = new AssetRepairFilter();
                if (!string.IsNullOrEmpty(employeeNo))
                {
                    filter.EmployeeNo = employeeNo;
                }

                var repairs = await GetAssetRepairsAsync(filter);

                return new AssetRepairSummary
                {
                    TotalRepairs = repairs.Count,
                    OpenRepairs = repairs.Count(r => r.MaintenanceStatus == RepairStatus.OPEN),
                    InProgressRepairs = repairs.Count(r => r.MaintenanceStatus == RepairStatus.IN_PROGRESS),
                    CompletedRepairs = repairs.Count(r => r.MaintenanceStatus == RepairStatus.COMPLETED),
                    CriticalRepairs = repairs.Count(r => r.RepairPriority == RepairPriority.CRITICAL),
                    TotalEstimatedCost = repairs.Sum(r => r.EstimatedCost ?? 0),
                    TotalActualCost = repairs.Sum(r => r.ActualCost ?? 0)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calculating repair summary");
                return new AssetRepairSummary();
            }
        }

        public async Task<List<string>> GetRepairStatusListAsync()
        {
            return await Task.FromResult(new List<string>
            {
                RepairStatus.OPEN,
                RepairStatus.IN_PROGRESS,
                RepairStatus.COMPLETED,
                RepairStatus.CANCELLED,
                RepairStatus.ON_HOLD
            });
        }

        public async Task<List<string>> GetRepairPriorityListAsync()
        {
            return await Task.FromResult(new List<string>
            {
                RepairPriority.LOW,
                RepairPriority.MEDIUM,
                RepairPriority.HIGH,
                RepairPriority.CRITICAL
            });
        }

        #endregion

        #region Dashboard Operations

        public async Task<int> GetOpenRepairsCountAsync()
        {
            try
            {
                var filter = new AssetRepairFilter
                {
                    MaintenanceStatus = RepairStatus.OPEN
                };

                var repairs = await GetAssetRepairsAsync(filter);
                return repairs.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching open repairs count");
                return 0;
            }
        }

        public async Task<int> GetCriticalRepairsCountAsync()
        {
            try
            {
                var filter = new AssetRepairFilter
                {
                    RepairPriority = RepairPriority.CRITICAL,
                    MaintenanceStatus = RepairStatus.OPEN
                };

                var repairs = await GetAssetRepairsAsync(filter);
                return repairs.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching critical repairs count");
                return 0;
            }
        }

        public async Task<List<AssetRepair>> GetRecentRepairsAsync(int count = 10)
        {
            try
            {
                var filter = new AssetRepairFilter
                {
                    OrderBy = "ReportedDate desc"
                };

                var repairs = await GetAssetRepairsAsync(filter);
                return repairs.Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching recent repairs");
                return new List<AssetRepair>();
            }
        }

        #endregion

        #region Assignment and Workflow

        public async Task<string> AssignRepairAsync(string maintenanceRefNo, string technician)
        {
            try
            {
                var repair = await GetAssetRepairAsync(maintenanceRefNo);
                if (repair == null)
                {
                    return $"Error: Repair request {maintenanceRefNo} not found";
                }

                repair.AssignedTechnician = technician;
                repair.MaintenanceStatus = RepairStatus.IN_PROGRESS;

                return await UpdateAssetRepairAsync(repair);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error assigning repair {maintenanceRefNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateRepairStatusAsync(string maintenanceRefNo, string status, string? resolution = null)
        {
            try
            {
                var repair = await GetAssetRepairAsync(maintenanceRefNo);
                if (repair == null)
                {
                    return $"Error: Repair request {maintenanceRefNo} not found";
                }

                repair.MaintenanceStatus = status;

                if (!string.IsNullOrEmpty(resolution))
                {
                    repair.ResolutionDetails = resolution;
                }

                if (status == RepairStatus.COMPLETED)
                {
                    repair.CompletedDate = DateTime.Now;
                }

                return await UpdateAssetRepairAsync(repair);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating repair status for {maintenanceRefNo}");
                return $"Error: {ex.Message}";
            }
        }

        #endregion

        #region Export Operations

        public async Task<byte[]> ExportRepairsToExcelAsync(AssetRepairFilter filter)
        {
            try
            {
                var repairs = await GetAssetRepairsAsync(filter);

                using var package = new OfficeOpenXml.ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Asset Repairs");

                // Add headers
                worksheet.Cells[1, 1].Value = "Ref No";
                worksheet.Cells[1, 2].Value = "Reported Date";
                worksheet.Cells[1, 3].Value = "Asset No";
                worksheet.Cells[1, 4].Value = "Asset Name";
                worksheet.Cells[1, 5].Value = "Employee";
                worksheet.Cells[1, 6].Value = "Department";
                worksheet.Cells[1, 7].Value = "Issue";
                worksheet.Cells[1, 8].Value = "Priority";
                worksheet.Cells[1, 9].Value = "Status";
                worksheet.Cells[1, 10].Value = "Estimated Cost";
                worksheet.Cells[1, 11].Value = "Actual Cost";
                worksheet.Cells[1, 12].Value = "Assigned To";
                worksheet.Cells[1, 13].Value = "Completed Date";

                // Style headers
                using (var range = worksheet.Cells[1, 1, 1, 13])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                // Add data
                int row = 2;
                foreach (var repair in repairs)
                {
                    worksheet.Cells[row, 1].Value = repair.MaintenanceRefNo;
                    worksheet.Cells[row, 2].Value = repair.ReportedDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 3].Value = repair.AssetNo;
                    worksheet.Cells[row, 4].Value = repair.AssetName;
                    worksheet.Cells[row, 5].Value = repair.EmployeeName;
                    worksheet.Cells[row, 6].Value = repair.Department;
                    worksheet.Cells[row, 7].Value = repair.MaintenanceIssue;
                    worksheet.Cells[row, 8].Value = repair.RepairPriority;
                    worksheet.Cells[row, 9].Value = repair.MaintenanceStatus;
                    worksheet.Cells[row, 10].Value = repair.EstimatedCost;
                    worksheet.Cells[row, 11].Value = repair.ActualCost;
                    worksheet.Cells[row, 12].Value = repair.AssignedTechnician;
                    worksheet.Cells[row, 13].Value = repair.CompletedDate?.ToString("yyyy-MM-dd");
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return await Task.FromResult(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error exporting repairs to Excel");
                return Array.Empty<byte>();
            }
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
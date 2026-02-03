using KNQASelfService.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace KNQASelfService.Services
{
    public class FixedAssetService : IFixedAssetService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FixedAssetService> _logger;
        private readonly string _baseUrl;
        private readonly string _authHeaderValue;

        public FixedAssetService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<FixedAssetService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _baseUrl = _configuration["BusinessCentral:BaseUrl"] ?? "";
            var username = _configuration["BusinessCentral:Username"] ?? "";
            var password = _configuration["BusinessCentral:Password"] ?? "";

            // Create auth header once
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

        private string BuildFilterQuery(FixedAssetFilter filter)
        {
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.Replace("'", "''");
                filters.Add($"(contains(No, '{searchTerm}') or contains(Description, '{searchTerm}') or contains(TagNo, '{searchTerm}') or contains(SerialNo, '{searchTerm}'))");
            }

            if (!string.IsNullOrWhiteSpace(filter.FixedAssetClassCode))
            {
                filters.Add($"FixedAssetClassCode eq '{filter.FixedAssetClassCode.Replace("'", "''")}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.TangibleIntangible))
            {
                filters.Add($"TangibleIntangible eq '{filter.TangibleIntangible}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.Location))
            {
                filters.Add($"Location eq '{filter.Location.Replace("'", "''")}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.ResponsibleEmployee))
            {
                filters.Add($"ResponsibleEmployee eq '{filter.ResponsibleEmployee}'");
            }

            if (!string.IsNullOrWhiteSpace(filter.DepartmentCode))
            {
                filters.Add($"DepartmentCode eq '{filter.DepartmentCode.Replace("'", "''")}'");
            }

            if (filter.ActiveOnly.HasValue)
            {
                filters.Add($"Active eq {filter.ActiveOnly.Value.ToString().ToLower()}");
            }

            if (!string.IsNullOrWhiteSpace(filter.AllocationType))
            {
                if (filter.AllocationType == AssetAllocationType.UNALLOCATED)
                {
                    filters.Add("ResponsibleEmployee eq ''");
                }
                else
                {
                    filters.Add($"AllocationType eq '{filter.AllocationType}'");
                }
            }

            if (filter.AcqDateFrom.HasValue)
            {
                filters.Add($"AcqDate ge {filter.AcqDateFrom.Value:yyyy-MM-dd}");
            }

            if (filter.AcqDateTo.HasValue)
            {
                filters.Add($"AcqDate le {filter.AcqDateTo.Value:yyyy-MM-dd}");
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

            // Add $top for performance if no specific limit
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

        #endregion

        #region Employee Methods

        public async Task<List<Employee>> GetEmployeesAsync()
        {
            try
            {
                var url = GetFullUrl("Company('KNQA')/Employees?$filter=Status eq 'Active'&$select=No,FullName,Status");

                SetupRequestHeaders();

                _logger.LogInformation($"🔍 Fetching employees from: {url}");

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"✅ Employees API response received. Content length: {content.Length}");

                    try
                    {
                        var result = JsonConvert.DeserializeObject<ODataResponse<Employee>>(content);
                        if (result?.Value != null)
                        {
                            var validEmployees = result.Value
                                .Where(e => !string.IsNullOrEmpty(e.No))
                                .ToList();

                            _logger.LogInformation($"✅ Successfully loaded {validEmployees.Count} employees");

                            // Log first few employees for debugging
                            foreach (var emp in validEmployees.Take(5))
                            {
                                _logger.LogDebug($"   Employee: {emp.No} - {emp.FullName}");
                            }

                            return validEmployees;
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Employees API returned null or empty value");
                        }
                    }
                    catch (Newtonsoft.Json.JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "❌ Error deserializing employees with Newtonsoft.Json");
                        _logger.LogError($"Response content: {content}");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to fetch employees. Status: {response.StatusCode}, Response: {errorContent}");
                }

                return new List<Employee>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting employees");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return new List<Employee>();
            }
        }

        public async Task<List<Employee>> GetActiveEmployeesAsync()
        {
            try
            {
                var allEmployees = await GetEmployeesAsync();
                return allEmployees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching active employees");
                return new List<Employee>();
            }
        }

        public async Task<Employee?> GetEmployeeAsync(string employeeNo)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/Employees('{employeeNo}')?$select=No,FullName,Status");

                SetupRequestHeaders();

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

        #endregion

        #region Fixed Asset Methods

        public async Task<List<FixedAsset>> GetFixedAssetsAsync(FixedAssetFilter filter)
        {
            try
            {
                var queryParams = BuildFilterQuery(filter);
                var url = GetFullUrl($"Company('KNQA')/FixedAssets{queryParams}");

                SetupRequestHeaders();

                _logger.LogInformation($"🔍 Fetching fixed assets from: {url}");

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"✅ Fixed Assets API response received. Content length: {content.Length}");

                    try
                    {
                        var result = JsonConvert.DeserializeObject<ODataResponse<FixedAsset>>(content);
                        if (result?.Value != null)
                        {
                            _logger.LogInformation($"✅ Successfully loaded {result.Value.Count} fixed assets");
                            return result.Value;
                        }
                    }
                    catch (Newtonsoft.Json.JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "❌ Error deserializing fixed assets");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to fetch fixed assets. Status: {response.StatusCode}");
                    _logger.LogError($"❌ Error response: {errorContent}");
                    _logger.LogError($"❌ URL attempted: {url}");
                }

                return new List<FixedAsset>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching fixed assets");
                return new List<FixedAsset>();
            }
        }

        public async Task<List<FixedAsset>> GetAssetsByEmployeeAsync(string employeeNo)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/FixedAssets?$filter=ResponsibleEmployee eq '{employeeNo}'");

                SetupRequestHeaders();

                _logger.LogInformation($"🔍 Fetching assets for employee: {employeeNo} from: {url}");

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var result = JsonConvert.DeserializeObject<ODataResponse<FixedAsset>>(content);
                        return result?.Value ?? new List<FixedAsset>();
                    }
                    catch (Newtonsoft.Json.JsonException)
                    {
                        return new List<FixedAsset>();
                    }
                }
                else
                {
                    _logger.LogError($"❌ Failed to fetch assets for employee. Status: {response.StatusCode}");
                    return new List<FixedAsset>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching assets for employee {employeeNo}");
                return new List<FixedAsset>();
            }
        }

        public async Task<FixedAsset?> GetFixedAssetAsync(string assetNo)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/FixedAssets('{assetNo}')");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var asset = JsonConvert.DeserializeObject<FixedAsset>(content);

                        // Get ETag if available
                        if (response.Headers.TryGetValues("ETag", out var etagValues))
                        {
                            var etag = etagValues.FirstOrDefault();
                            if (!string.IsNullOrEmpty(etag))
                            {
                                etag = etag.Trim('"');
                                if (asset != null) asset.ODataEtag = etag;
                            }
                        }

                        return asset;
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
                _logger.LogError(ex, $"❌ Error fetching asset {assetNo}");
                return null;
            }
        }

        public async Task<FixedAssetSummary> GetAssetSummaryAsync(string? employeeNo = null)
        {
            try
            {
                var filter = new FixedAssetFilter();
                if (!string.IsNullOrEmpty(employeeNo))
                {
                    filter.ResponsibleEmployee = employeeNo;
                }

                var assets = await GetFixedAssetsAsync(filter);

                return new FixedAssetSummary
                {
                    TotalAssets = assets.Count,
                    ActiveAssets = assets.Count(a => a.Active),
                    InactiveAssets = assets.Count(a => !a.Active),
                    AllocatedAssets = assets.Count(a => !string.IsNullOrEmpty(a.ResponsibleEmployee)),
                    UnallocatedAssets = assets.Count(a => string.IsNullOrEmpty(a.ResponsibleEmployee)),
                    TangibleAssets = assets.Count(a => a.TangibleIntangible == AssetType.TANGIBLE),
                    IntangibleAssets = assets.Count(a => a.TangibleIntangible == AssetType.INTANGIBLE),
                    TotalAcquisitionCost = assets.Sum(a => a.AcquisitionCost),
                    TotalBookValue = assets.Sum(a => a.BookValue)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calculating asset summary");
                return new FixedAssetSummary();
            }
        }

        #endregion

        #region Master Data Methods

        public async Task<List<string>> GetAssetClassesAsync()
        {
            try
            {
                var url = GetFullUrl("Company('KNQA')/FixedAssetClasses?$select=Code,Description&$top=100");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var result = JsonConvert.DeserializeObject<ODataResponse<FixedAssetClassResponse>>(content);
                        if (result?.Value != null && result.Value.Any())
                        {
                            return result.Value.Select(x => x.Description ?? x.Code).Distinct().ToList();
                        }
                    }
                    catch (Newtonsoft.Json.JsonException)
                    {
                        // Ignore and use fallback
                    }
                }

                // Fallback to predefined classes
                return await Task.FromResult(new List<string>
                {
                    FixedAssetClass.COMPUTER_EQUIPMENT,
                    FixedAssetClass.FURNITURE,
                    FixedAssetClass.VEHICLES,
                    FixedAssetClass.MACHINERY,
                    FixedAssetClass.BUILDINGS,
                    FixedAssetClass.LAND,
                    FixedAssetClass.SOFTWARE,
                    FixedAssetClass.LICENSES,
                    FixedAssetClass.OFFICE_EQUIPMENT,
                    FixedAssetClass.OTHER
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching asset classes");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetLocationsAsync()
        {
            try
            {
                var url = GetFullUrl("Company('KNQA')/Locations?$select=Code,Name&$top=100");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var result = JsonConvert.DeserializeObject<ODataResponse<LocationResponse>>(content);
                        if (result?.Value != null && result.Value.Any())
                        {
                            return result.Value.Select(x => x.Name ?? x.Code).Distinct().ToList();
                        }
                    }
                    catch (Newtonsoft.Json.JsonException)
                    {
                        // Ignore and use fallback
                    }
                }

                // Fallback to sample locations
                return await Task.FromResult(new List<string>
                {
                    "Head Office",
                    "Branch Office - Nairobi",
                    "Branch Office - Mombasa",
                    "Branch Office - Kisumu",
                    "Warehouse",
                    "Training Center",
                    "Remote"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching locations");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetDepartmentsAsync()
        {
            try
            {
                var url = GetFullUrl("Company('KNQA')/Departments?$select=Code,Name&$top=100");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var result = JsonConvert.DeserializeObject<ODataResponse<DepartmentResponse>>(content);
                        if (result?.Value != null && result.Value.Any())
                        {
                            return result.Value.Select(x => x.Name ?? x.Code).Distinct().ToList();
                        }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // Ignore and use fallback
                    }
                }

                // Fallback to sample departments
                return await Task.FromResult(new List<string>
                {
                    "IT Department",
                    "Finance Department",
                    "HR Department",
                    "Operations",
                    "Quality Assurance",
                    "Administration",
                    "Marketing",
                    "Training"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching departments");
                return new List<string>();
            }
        }

        #endregion

        #region CRUD Operations

        public async Task<string> CreateFixedAssetAsync(FixedAsset asset)
        {
            try
            {
                var url = GetFullUrl("Company('KNQA')/FixedAssets");

                SetupRequestHeaders();

                var json = JsonConvert.SerializeObject(new
                {
                    No = asset.No,
                    Description = asset.Description,
                    FixedAssetClassCode = asset.FixedAssetClassCode,
                    TangibleIntangible = asset.TangibleIntangible,
                    ClassCode = asset.ClassCode,
                    SubclassCode = asset.SubclassCode,
                    Location = asset.Location,
                    TagNo = asset.TagNo,
                    SerialNo = asset.SerialNo,
                    RegistrationNo = asset.RegistrationNo,
                    Active = asset.Active,
                    AcqDate = asset.AcqDate,
                    SearchDescription = asset.SearchDescription,
                    AllocationType = asset.AllocationType,
                    EmployeeCustomer = asset.EmployeeCustomer,
                    ResponsibleEmployee = asset.ResponsibleEmployee,
                    DepartmentCode = asset.DepartmentCode,
                    KnqaCode = asset.KnqaCode,
                    Manufacturer = asset.Manufacturer,
                    Model = asset.Model,
                    Condition = asset.Condition,
                    WarrantyExpiryDate = asset.WarrantyExpiryDate,
                    AcquisitionCost = asset.AcquisitionCost,
                    BookValue = asset.BookValue,
                    LastMaintenanceDate = asset.LastMaintenanceDate,
                    NextMaintenanceDate = asset.NextMaintenanceDate,
                    Remarks = asset.Remarks
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"📤 Creating fixed asset: {asset.No}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Fixed asset {asset.No} created successfully");
                    return "Success: Fixed asset created successfully";
                }
                else
                {
                    _logger.LogError($"❌ Failed to create asset. Status: {response.StatusCode}, Response: {responseContent}");
                    return ParseErrorResponse(responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error creating fixed asset {asset.No}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateFixedAssetAsync(FixedAsset asset)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/FixedAssets('{asset.No}')");

                SetupRequestHeaders();

                var json = JsonConvert.SerializeObject(new
                {
                    Description = asset.Description,
                    FixedAssetClassCode = asset.FixedAssetClassCode,
                    TangibleIntangible = asset.TangibleIntangible,
                    ClassCode = asset.ClassCode,
                    SubclassCode = asset.SubclassCode,
                    Location = asset.Location,
                    TagNo = asset.TagNo,
                    SerialNo = asset.SerialNo,
                    RegistrationNo = asset.RegistrationNo,
                    Active = asset.Active,
                    AcqDate = asset.AcqDate,
                    SearchDescription = asset.SearchDescription,
                    AllocationType = asset.AllocationType,
                    EmployeeCustomer = asset.EmployeeCustomer,
                    ResponsibleEmployee = asset.ResponsibleEmployee,
                    DepartmentCode = asset.DepartmentCode,
                    KnqaCode = asset.KnqaCode,
                    Manufacturer = asset.Manufacturer,
                    Model = asset.Model,
                    Condition = asset.Condition,
                    WarrantyExpiryDate = asset.WarrantyExpiryDate,
                    AcquisitionCost = asset.AcquisitionCost,
                    BookValue = asset.BookValue,
                    LastMaintenanceDate = asset.LastMaintenanceDate,
                    NextMaintenanceDate = asset.NextMaintenanceDate,
                    Remarks = asset.Remarks
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Add If-Match header for concurrency control
                if (!string.IsNullOrEmpty(asset.ODataEtag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", asset.ODataEtag);
                }

                _logger.LogInformation($"🔄 Updating fixed asset: {asset.No}");

                var response = await _httpClient.PatchAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Fixed asset {asset.No} updated successfully");
                    return "Success: Fixed asset updated successfully";
                }
                else
                {
                    _logger.LogError($"❌ Failed to update asset. Status: {response.StatusCode}, Response: {responseContent}");
                    return ParseErrorResponse(responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating fixed asset {asset.No}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> DeleteFixedAssetAsync(string assetNo)
        {
            try
            {
                _logger.LogInformation($"🗑️ Deleting fixed asset: {assetNo}");

                // First get the asset to get the etag
                var asset = await GetFixedAssetAsync(assetNo);
                if (asset == null)
                {
                    return $"Error: Asset {assetNo} not found";
                }

                var url = GetFullUrl($"Company('KNQA')/FixedAssets('{assetNo}')");

                SetupRequestHeaders();

                // Add If-Match header for concurrency control
                if (!string.IsNullOrEmpty(asset.ODataEtag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", asset.ODataEtag);
                }

                var response = await _httpClient.DeleteAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Fixed asset {assetNo} deleted successfully");
                    return "Success: Fixed asset deleted successfully";
                }
                else
                {
                    _logger.LogError($"❌ Failed to delete asset. Status: {response.StatusCode}, Response: {responseContent}");
                    return ParseErrorResponse(responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting fixed asset {assetNo}");
                return $"Error: {ex.Message}";
            }
        }

        #endregion

        #region Other Methods

        public async Task<string> UpdateAssetAllocationAsync(FixedAssetAllocation allocation, string etag)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/FixedAssets('{allocation.AssetNo}')");

                SetupRequestHeaders();

                var json = JsonConvert.SerializeObject(new
                {
                    ResponsibleEmployee = allocation.ResponsibleEmployee,
                    DepartmentCode = allocation.DepartmentCode,
                    Location = allocation.Location
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Add If-Match header for concurrency control
                if (!string.IsNullOrEmpty(etag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                _logger.LogInformation($"🔄 Updating allocation for asset: {allocation.AssetNo}");

                var response = await _httpClient.PatchAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Asset {allocation.AssetNo} allocation updated successfully");
                    return $"Success: Asset {allocation.AssetNo} allocation updated successfully";
                }
                else
                {
                    _logger.LogError($"❌ Failed to update allocation. Status: {response.StatusCode}, Response: {responseContent}");
                    return ParseErrorResponse(responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating allocation for asset {allocation.AssetNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<List<AssetMaintenance>> GetAssetMaintenanceHistoryAsync(string assetNo)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/AssetMaintenance?$filter=AssetNo eq '{assetNo}'&$orderby=MaintenanceDate desc");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"❌ Failed to fetch maintenance history. Status: {response.StatusCode}");
                    return new List<AssetMaintenance>();
                }

                var content = await response.Content.ReadAsStringAsync();

                try
                {
                    var result = JsonConvert.DeserializeObject<ODataResponse<AssetMaintenance>>(content);
                    return result?.Value ?? new List<AssetMaintenance>();
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    return new List<AssetMaintenance>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching maintenance history for asset {assetNo}");
                return new List<AssetMaintenance>();
            }
        }

        public async Task<string> AddMaintenanceRecordAsync(AssetMaintenance maintenance)
        {
            try
            {
                var url = GetFullUrl("Company('KNQA')/AssetMaintenance");

                SetupRequestHeaders();

                var json = JsonConvert.SerializeObject(new
                {
                    AssetNo = maintenance.AssetNo,
                    MaintenanceDate = maintenance.MaintenanceDate,
                    MaintenanceType = maintenance.MaintenanceType,
                    Description = maintenance.Description,
                    Cost = maintenance.Cost,
                    PerformedBy = maintenance.PerformedBy,
                    NextMaintenanceDate = maintenance.NextMaintenanceDate,
                    Remarks = maintenance.Remarks
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"📝 Adding maintenance record for asset: {maintenance.AssetNo}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Maintenance record added successfully for asset {maintenance.AssetNo}");
                    return "Success: Maintenance record added successfully";
                }
                else
                {
                    _logger.LogError($"❌ Failed to add maintenance record. Status: {response.StatusCode}, Response: {responseContent}");
                    return ParseErrorResponse(responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error adding maintenance record");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<FixedAsset?> SearchByTagNoAsync(string tagNo)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/FixedAssets?$filter=TagNo eq '{tagNo}'");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();

                try
                {
                    var result = JsonConvert.DeserializeObject<ODataResponse<FixedAsset>>(content);
                    return result?.Value?.FirstOrDefault();
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error searching by tag {tagNo}");
                return null;
            }
        }

        public async Task<FixedAsset?> SearchBySerialNoAsync(string serialNo)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/FixedAssets?$filter=SerialNo eq '{serialNo}'");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();

                try
                {
                    var result = JsonConvert.DeserializeObject<ODataResponse<FixedAsset>>(content);
                    return result?.Value?.FirstOrDefault();
                }
                catch (System.Text.Json.JsonException)
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error searching by serial {serialNo}");
                return null;
            }
        }

        public async Task<List<FixedAsset>> GetAssetsDueForMaintenanceAsync(int daysAhead = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(daysAhead).ToString("yyyy-MM-dd");
                var url = GetFullUrl($"Company('KNQA')/FixedAssets?$filter=NextMaintenanceDate le {cutoffDate} and Active eq true");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"❌ Failed to fetch assets due for maintenance. Status: {response.StatusCode}");
                    return new List<FixedAsset>();
                }

                var content = await response.Content.ReadAsStringAsync();

                try
                {
                    var result = JsonConvert.DeserializeObject<ODataResponse<FixedAsset>>(content);
                    return result?.Value ?? new List<FixedAsset>();
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    return new List<FixedAsset>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching assets due for maintenance");
                return new List<FixedAsset>();
            }
        }

        public async Task<byte[]> ExportAssetsToExcelAsync(FixedAssetFilter filter)
        {
            try
            {
                var assets = await GetFixedAssetsAsync(filter);

                using var package = new OfficeOpenXml.ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Fixed Assets");

                // Add headers
                worksheet.Cells[1, 1].Value = "Asset No";
                worksheet.Cells[1, 2].Value = "Description";
                worksheet.Cells[1, 3].Value = "Asset Class";
                worksheet.Cells[1, 4].Value = "Type";
                worksheet.Cells[1, 5].Value = "Tag No";
                worksheet.Cells[1, 6].Value = "Serial No";
                worksheet.Cells[1, 7].Value = "Location";
                worksheet.Cells[1, 8].Value = "Responsible Employee";
                worksheet.Cells[1, 9].Value = "Department";
                worksheet.Cells[1, 10].Value = "Acquisition Cost";
                worksheet.Cells[1, 11].Value = "Book Value";
                worksheet.Cells[1, 12].Value = "Acquisition Date";
                worksheet.Cells[1, 13].Value = "Status";

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
                foreach (var asset in assets)
                {
                    worksheet.Cells[row, 1].Value = asset.No;
                    worksheet.Cells[row, 2].Value = asset.Description;
                    worksheet.Cells[row, 3].Value = asset.FixedAssetClassCode;
                    worksheet.Cells[row, 4].Value = asset.TangibleIntangible;
                    worksheet.Cells[row, 5].Value = asset.TagNo;
                    worksheet.Cells[row, 6].Value = asset.SerialNo;
                    worksheet.Cells[row, 7].Value = asset.Location;
                    worksheet.Cells[row, 8].Value = asset.ResponsibleEmployeeName;
                    worksheet.Cells[row, 9].Value = asset.DepartmentCode;
                    worksheet.Cells[row, 10].Value = asset.AcquisitionCost;
                    worksheet.Cells[row, 11].Value = asset.BookValue;
                    worksheet.Cells[row, 12].Value = asset.AcqDate;
                    worksheet.Cells[row, 13].Value = asset.Active ? "Active" : "Inactive";
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return await Task.FromResult(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error exporting assets to Excel");
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

        private class FixedAssetClassResponse
        {
            public string? Code { get; set; }
            public string? Description { get; set; }
        }

        private class LocationResponse
        {
            public string? Code { get; set; }
            public string? Name { get; set; }
        }

        private class DepartmentResponse
        {
            public string? Code { get; set; }
            public string? Name { get; set; }
        }

        #endregion
    }
}
// Services/VehicleMaintenanceService.cs
using KNQASelfService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KNQASelfService.Services
{
    public class VehicleMaintenanceService : IVehicleMaintenanceService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ICurrentUserService _currentUserService;

        public VehicleMaintenanceService(HttpClient httpClient, IConfiguration configuration, ICurrentUserService currentUserService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _currentUserService = currentUserService;

            // Configure base address and headers
            var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://197.232.37.154:8048/BC252/ODataV4/";
            _httpClient.BaseAddress = new Uri(baseUrl);

            // Set authentication
            var username = _configuration["ApiSettings:Username"];
            var password = _configuration["ApiSettings:Password"];
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            }

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<VehicleMaintenance>> GetAllMaintenanceRecordsAsync()
        {
            try
            {
                var company = _configuration["ApiSettings:Company"] ?? "KNQA";
                var response = await _httpClient.GetAsync($"Company('{company}')/VehicleMaintenance");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ODataResponse<VehicleMaintenance>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result?.Value ?? new List<VehicleMaintenance>();
                }
                else
                {
                    Console.WriteLine($"Error fetching maintenance records: {response.StatusCode}");
                    return new List<VehicleMaintenance>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetAllMaintenanceRecordsAsync: {ex.Message}");
                return new List<VehicleMaintenance>();
            }
        }

        public async Task<List<VehicleMaintenance>> GetMaintenanceByVehicleAsync(string vehicleNo)
        {
            try
            {
                var company = _configuration["ApiSettings:Company"] ?? "KNQA";
                var response = await _httpClient.GetAsync($"Company('{company}')/VehicleMaintenance?$filter=Vehicle_Registration_No eq '{vehicleNo}'");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ODataResponse<VehicleMaintenance>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result?.Value ?? new List<VehicleMaintenance>();
                }
                else
                {
                    Console.WriteLine($"Error fetching maintenance by vehicle: {response.StatusCode}");
                    return new List<VehicleMaintenance>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetMaintenanceByVehicleAsync: {ex.Message}");
                return new List<VehicleMaintenance>();
            }
        }

        public async Task<List<VehicleMaintenance>> GetMaintenanceByDateRangeAsync(string fromDate, string toDate)
        {
            try
            {
                var company = _configuration["ApiSettings:Company"] ?? "KNQA";
                var response = await _httpClient.GetAsync($"Company('{company}')/VehicleMaintenance?$filter=Maintenance_Date ge {fromDate} and Maintenance_Date le {toDate}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ODataResponse<VehicleMaintenance>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result?.Value ?? new List<VehicleMaintenance>();
                }
                else
                {
                    Console.WriteLine($"Error fetching maintenance by date range: {response.StatusCode}");
                    return new List<VehicleMaintenance>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetMaintenanceByDateRangeAsync: {ex.Message}");
                return new List<VehicleMaintenance>();
            }
        }

        public async Task<VehicleMaintenance> GetMaintenanceRecordAsync(string maintenanceNo)
        {
            try
            {
                var company = _configuration["ApiSettings:Company"] ?? "KNQA";
                var response = await _httpClient.GetAsync($"Company('{company}')/VehicleMaintenance('{maintenanceNo}')");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<VehicleMaintenance>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result;
                }
                else
                {
                    Console.WriteLine($"Error fetching maintenance record: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetMaintenanceRecordAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<string> CreateMaintenanceRecordAsync(VehicleMaintenanceCreate maintenance)
        {
            try
            {
                var company = _configuration["ApiSettings:Company"] ?? "KNQA";
                var url = $"Company('{company}')/VehicleMaintenance";

                // Calculate derived fields
                var vehicleMaintenance = new VehicleMaintenance
                {
                    FACodeNo = maintenance.FACodeNo,
                    VehicleRegistrationNo = maintenance.VehicleRegistrationNo,
                    MaintenanceDate = maintenance.MaintenanceDate,
                    MaintenanceType = maintenance.MaintenanceType,
                    PreServiceMileage = maintenance.PreServiceMileage,
                    TotalRepairCost = maintenance.TotalRepairCost,
                    TotalMaintenanceCost = maintenance.TotalMaintenanceCost,
                    TotalCost = maintenance.TotalRepairCost + maintenance.TotalMaintenanceCost,
                    MaintenanceVendorNo = maintenance.MaintenanceVendorNo,
                    ServiceDescription = maintenance.ServiceDescription,
                    NextServiceDate = maintenance.NextServiceDate,
                    NextServiceMileage = maintenance.NextServiceMileage,
                    Remarks = maintenance.Remarks,
                    CreatedBy = maintenance.CreatedBy,
                    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    CreatedTime = DateTime.Now.ToString("HH:mm:ss"),
                    Status = MaintenanceStatus.NEW
                };

                var json = JsonSerializer.Serialize(vehicleMaintenance, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return "Success: Maintenance record created successfully";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error creating maintenance: {response.StatusCode} - {errorContent}");
                    return $"Error: Failed to create maintenance record. Status: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in CreateMaintenanceRecordAsync: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateMaintenanceRecordAsync(VehicleMaintenance maintenance)
        {
            try
            {
                var company = _configuration["ApiSettings:Company"] ?? "KNQA";
                var url = $"Company('{company}')/VehicleMaintenance('{maintenance.No}')";

                // Recalculate total cost
                maintenance.TotalCost = maintenance.TotalRepairCost + maintenance.TotalMaintenanceCost;

                // Calculate mileage difference if both pre and post service mileage are available
                if (maintenance.PreServiceMileage > 0 && maintenance.PostServiceMileage > 0)
                {
                    maintenance.MileageDifference = maintenance.PostServiceMileage - maintenance.PreServiceMileage;
                }

                var json = JsonSerializer.Serialize(maintenance, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var request = new HttpRequestMessage(HttpMethod.Patch, url);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!string.IsNullOrEmpty(maintenance.ODataEtag))
                {
                    request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{maintenance.ODataEtag}\""));
                }

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return "Success: Maintenance record updated successfully";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                {
                    return "Error: Record has been modified by another user. Please refresh and try again.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error updating maintenance: {response.StatusCode} - {errorContent}");
                    return $"Error: Failed to update maintenance record. Status: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateMaintenanceRecordAsync: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<bool> DeleteMaintenanceRecordAsync(string maintenanceNo, string etag)
        {
            try
            {
                var company = _configuration["ApiSettings:Company"] ?? "KNQA";
                var url = $"Company('{company}')/VehicleMaintenance('{maintenanceNo}')";

                var request = new HttpRequestMessage(HttpMethod.Delete, url);
                if (!string.IsNullOrEmpty(etag))
                {
                    request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{etag}\""));
                }

                var response = await _httpClient.SendAsync(request);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in DeleteMaintenanceRecordAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<string> UpdateMaintenanceStatusAsync(string maintenanceNo, string status, string remarks = "")
        {
            try
            {
                var maintenance = await GetMaintenanceRecordAsync(maintenanceNo);
                if (maintenance == null)
                    return "Error: Maintenance record not found";

                maintenance.Status = status;
                if (!string.IsNullOrEmpty(remarks))
                {
                    maintenance.Remarks = string.IsNullOrEmpty(maintenance.Remarks)
                        ? remarks
                        : $"{maintenance.Remarks}\nStatus Update: {remarks}";
                }

                return await UpdateMaintenanceRecordAsync(maintenance);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateMaintenanceStatusAsync: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<List<FleetVehicle>> GetFleetVehiclesAsync()
        {
            try
            {
                var company = _configuration["ApiSettings:Company"] ?? "KNQA";
                var response = await _httpClient.GetAsync($"Company('{company}')/Fleet_List");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ODataResponse<FleetVehicle>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result?.Value ?? new List<FleetVehicle>();
                }
                else
                {
                    Console.WriteLine($"Error fetching fleet vehicles: {response.StatusCode}");
                    return new List<FleetVehicle>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetFleetVehiclesAsync: {ex.Message}");
                return new List<FleetVehicle>();
            }
        }

        public async Task<FleetVehicle> GetVehicleDetailsAsync(string vehicleNo)
        {
            try
            {
                var company = _configuration["ApiSettings:Company"] ?? "KNQA";
                var response = await _httpClient.GetAsync($"Company('{company}')/Fleet_List('{vehicleNo}')");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<FleetVehicle>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result;
                }
                else
                {
                    Console.WriteLine($"Error fetching vehicle details: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetVehicleDetailsAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<List<FleetVehicle>> GetVehiclesDueForMaintenanceAsync()
        {
            try
            {
                var vehicles = await GetFleetVehiclesAsync();
                var today = DateTime.Now;
                var dueVehicles = new List<FleetVehicle>();

                // For each vehicle, check the last maintenance record
                foreach (var vehicle in vehicles)
                {
                    var maintenanceRecords = await GetMaintenanceByVehicleAsync(vehicle.No);
                    var lastMaintenance = maintenanceRecords
                        .Where(m => m.Status == MaintenanceStatus.COMPLETED)
                        .OrderByDescending(m => m.MaintenanceDate)
                        .FirstOrDefault();

                    if (lastMaintenance != null && !string.IsNullOrEmpty(lastMaintenance.NextServiceDate))
                    {
                        if (DateTime.TryParse(lastMaintenance.NextServiceDate, out var nextServiceDate))
                        {
                            if (nextServiceDate <= today.AddDays(30) && nextServiceDate >= today)
                            {
                                dueVehicles.Add(vehicle);
                            }
                        }
                    }
                }

                return dueVehicles;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetVehiclesDueForMaintenanceAsync: {ex.Message}");
                return new List<FleetVehicle>();
            }
        }

        public async Task<MaintenanceSummary> GetMaintenanceSummaryAsync()
        {
            try
            {
                var allMaintenance = await GetAllMaintenanceRecordsAsync();
                var today = DateTime.Now;
                var thisMonth = today.Month;
                var thisYear = today.Year;

                var summary = new MaintenanceSummary
                {
                    TotalMaintenance = allMaintenance.Count,
                    CompletedMaintenance = allMaintenance.Count(m => m.Status == MaintenanceStatus.COMPLETED),
                    PendingMaintenance = allMaintenance.Count(m => m.Status == MaintenanceStatus.NEW ||
                                                                   m.Status == MaintenanceStatus.IN_PROGRESS ||
                                                                   m.Status == MaintenanceStatus.PENDING_APPROVAL),
                    OverdueMaintenance = await GetOverdueMaintenanceCountAsync(),
                    TotalCostThisMonth = allMaintenance
                        .Where(m => DateTime.TryParse(m.MaintenanceDate, out var date) &&
                                   date.Month == thisMonth && date.Year == thisYear)
                        .Sum(m => m.TotalCost),
                    TotalCostThisYear = allMaintenance
                        .Where(m => DateTime.TryParse(m.MaintenanceDate, out var date) && date.Year == thisYear)
                        .Sum(m => m.TotalCost)
                };

                // Group by type
                summary.MaintenanceByType = allMaintenance
                    .GroupBy(m => m.MaintenanceType)
                    .Select(g => new MaintenanceByType
                    {
                        MaintenanceType = g.Key,
                        Count = g.Count(),
                        TotalCost = g.Sum(m => m.TotalCost)
                    })
                    .ToList();

                // Group by vehicle
                summary.MaintenanceByVehicle = allMaintenance
                    .GroupBy(m => m.VehicleRegistrationNo)
                    .Select(g => new MaintenanceByVehicle
                    {
                        VehicleNo = g.Key,
                        MaintenanceCount = g.Count(),
                        TotalCost = g.Sum(m => m.TotalCost),
                        AverageCost = g.Average(m => m.TotalCost)
                    })
                    .ToList();

                // Get upcoming maintenance
                summary.UpcomingMaintenance = await GetUpcomingMaintenanceSummaryAsync();

                return summary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetMaintenanceSummaryAsync: {ex.Message}");
                return new MaintenanceSummary();
            }
        }

        private async Task<int> GetOverdueMaintenanceCountAsync()
        {
            try
            {
                var today = DateTime.Now.Date;
                var allMaintenance = await GetAllMaintenanceRecordsAsync();

                return allMaintenance.Count(m =>
                    !string.IsNullOrEmpty(m.NextServiceDate) &&
                    DateTime.TryParse(m.NextServiceDate, out var nextDate) &&
                    nextDate < today &&
                    (m.Status == MaintenanceStatus.NEW || m.Status == MaintenanceStatus.IN_PROGRESS));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetOverdueMaintenanceCountAsync: {ex.Message}");
                return 0;
            }
        }

        private async Task<List<UpcomingMaintenance>> GetUpcomingMaintenanceSummaryAsync()
        {
            try
            {
                var vehicles = await GetFleetVehiclesAsync();
                var upcoming = new List<UpcomingMaintenance>();

                foreach (var vehicle in vehicles)
                {
                    var maintenanceRecords = await GetMaintenanceByVehicleAsync(vehicle.No);
                    var lastMaintenance = maintenanceRecords
                        .Where(m => m.Status == MaintenanceStatus.COMPLETED)
                        .OrderByDescending(m => m.MaintenanceDate)
                        .FirstOrDefault();

                    if (lastMaintenance != null && !string.IsNullOrEmpty(lastMaintenance.NextServiceDate))
                    {
                        if (DateTime.TryParse(lastMaintenance.NextServiceDate, out var nextServiceDate))
                        {
                            upcoming.Add(new UpcomingMaintenance
                            {
                                VehicleNo = vehicle.No,
                                VehicleDescription = vehicle.Description,
                                RegistrationNo = vehicle.RegistrationNo,
                                NextServiceDate = lastMaintenance.NextServiceDate,
                                NextServiceMileage = lastMaintenance.NextServiceMileage,
                                CurrentMileage = lastMaintenance.PostServiceMileage,
                                Status = nextServiceDate <= DateTime.Now ? "Overdue" :
                                        nextServiceDate <= DateTime.Now.AddDays(7) ? "Due Soon" : "Upcoming"
                            });
                        }
                    }
                }

                return upcoming.OrderBy(u => u.NextServiceDate).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetUpcomingMaintenanceSummaryAsync: {ex.Message}");
                return new List<UpcomingMaintenance>();
            }
        }

        public async Task<MaintenanceSummary> GetVehicleMaintenanceSummaryAsync(string vehicleNo)
        {
            try
            {
                var vehicleMaintenance = await GetMaintenanceByVehicleAsync(vehicleNo);
                var vehicle = await GetVehicleDetailsAsync(vehicleNo);

                var summary = new MaintenanceSummary
                {
                    TotalMaintenance = vehicleMaintenance.Count,
                    CompletedMaintenance = vehicleMaintenance.Count(m => m.Status == MaintenanceStatus.COMPLETED),
                    PendingMaintenance = vehicleMaintenance.Count(m => m.Status == MaintenanceStatus.NEW ||
                                                                       m.Status == MaintenanceStatus.IN_PROGRESS ||
                                                                       m.Status == MaintenanceStatus.PENDING_APPROVAL),
                    TotalCostThisMonth = vehicleMaintenance
                        .Where(m => DateTime.TryParse(m.MaintenanceDate, out var date) &&
                                   date.Month == DateTime.Now.Month && date.Year == DateTime.Now.Year)
                        .Sum(m => m.TotalCost),
                    TotalCostThisYear = vehicleMaintenance
                        .Where(m => DateTime.TryParse(m.MaintenanceDate, out var date) &&
                                   date.Year == DateTime.Now.Year)
                        .Sum(m => m.TotalCost)
                };

                return summary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetVehicleMaintenanceSummaryAsync: {ex.Message}");
                return new MaintenanceSummary();
            }
        }

        public async Task<List<VehicleMaintenance>> GetUpcomingMaintenanceAsync(int days = 30)
        {
            try
            {
                var today = DateTime.Now;
                var futureDate = today.AddDays(days);
                var allMaintenance = await GetAllMaintenanceRecordsAsync();

                return allMaintenance.Where(m =>
                    !string.IsNullOrEmpty(m.NextServiceDate) &&
                    DateTime.TryParse(m.NextServiceDate, out var nextDate) &&
                    nextDate >= today && nextDate <= futureDate &&
                    (m.Status == MaintenanceStatus.NEW || m.Status == MaintenanceStatus.IN_PROGRESS))
                    .OrderBy(m => m.NextServiceDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetUpcomingMaintenanceAsync: {ex.Message}");
                return new List<VehicleMaintenance>();
            }
        }

        public async Task<List<VehicleMaintenance>> GetOverdueMaintenanceAsync()
        {
            try
            {
                var today = DateTime.Now;
                var allMaintenance = await GetAllMaintenanceRecordsAsync();

                return allMaintenance.Where(m =>
                    !string.IsNullOrEmpty(m.NextServiceDate) &&
                    DateTime.TryParse(m.NextServiceDate, out var nextDate) &&
                    nextDate < today &&
                    (m.Status == MaintenanceStatus.NEW || m.Status == MaintenanceStatus.IN_PROGRESS))
                    .OrderBy(m => m.NextServiceDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetOverdueMaintenanceAsync: {ex.Message}");
                return new List<VehicleMaintenance>();
            }
        }

        public async Task<decimal> GetMaintenanceCostByVehicleAsync(string vehicleNo, string fromDate = "", string toDate = "")
        {
            try
            {
                List<VehicleMaintenance> maintenance;

                if (string.IsNullOrEmpty(fromDate) && string.IsNullOrEmpty(toDate))
                {
                    maintenance = await GetMaintenanceByVehicleAsync(vehicleNo);
                }
                else
                {
                    maintenance = await GetMaintenanceByDateRangeAsync(fromDate, toDate);
                    maintenance = maintenance.Where(m => m.VehicleRegistrationNo == vehicleNo).ToList();
                }

                return maintenance.Sum(m => m.TotalCost);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetMaintenanceCostByVehicleAsync: {ex.Message}");
                return 0;
            }
        }

        public async Task<decimal> GetMaintenanceCostByPeriodAsync(string period)
        {
            try
            {
                var allMaintenance = await GetAllMaintenanceRecordsAsync();
                var today = DateTime.Now;

                return period.ToLower() switch
                {
                    "month" => allMaintenance
                        .Where(m => DateTime.TryParse(m.MaintenanceDate, out var date) &&
                                   date.Month == today.Month && date.Year == today.Year)
                        .Sum(m => m.TotalCost),
                    "quarter" => allMaintenance
                        .Where(m => DateTime.TryParse(m.MaintenanceDate, out var date) &&
                                   date.Year == today.Year &&
                                   (date.Month >= 1 && date.Month <= 3 && today.Month >= 1 && today.Month <= 3 ||
                                    date.Month >= 4 && date.Month <= 6 && today.Month >= 4 && today.Month <= 6 ||
                                    date.Month >= 7 && date.Month <= 9 && today.Month >= 7 && today.Month <= 9 ||
                                    date.Month >= 10 && date.Month <= 12 && today.Month >= 10 && today.Month <= 12))
                        .Sum(m => m.TotalCost),
                    "year" => allMaintenance
                        .Where(m => DateTime.TryParse(m.MaintenanceDate, out var date) && date.Year == today.Year)
                        .Sum(m => m.TotalCost),
                    _ => allMaintenance.Sum(m => m.TotalCost)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetMaintenanceCostByPeriodAsync: {ex.Message}");
                return 0;
            }
        }

        public async Task<List<MaintenanceByVehicle>> GetMaintenanceCostAnalysisAsync()
        {
            try
            {
                var vehicles = await GetFleetVehiclesAsync();
                var analysis = new List<MaintenanceByVehicle>();

                foreach (var vehicle in vehicles)
                {
                    var maintenance = await GetMaintenanceByVehicleAsync(vehicle.No);

                    analysis.Add(new MaintenanceByVehicle
                    {
                        VehicleNo = vehicle.No,
                        VehicleDescription = vehicle.Description,
                        RegistrationNo = vehicle.RegistrationNo,
                        MaintenanceCount = maintenance.Count,
                        TotalCost = maintenance.Sum(m => m.TotalCost),
                        AverageCost = maintenance.Any() ? maintenance.Average(m => m.TotalCost) : 0
                    });
                }

                return analysis.OrderByDescending(a => a.TotalCost).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetMaintenanceCostAnalysisAsync: {ex.Message}");
                return new List<MaintenanceByVehicle>();
            }
        }

        public async Task<List<object>> GetMaintenanceVendorsAsync()
        {
            try
            {
                var company = _configuration["ApiSettings:Company"] ?? "KNQA";
                var response = await _httpClient.GetAsync($"Company('{company}')/Vendors?$filter=Vendor_Type eq 'MAINTENANCE'");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ODataResponse<object>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result?.Value ?? new List<object>();
                }
                else
                {
                    Console.WriteLine($"Error fetching vendors: {response.StatusCode}");
                    return new List<object>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetMaintenanceVendorsAsync: {ex.Message}");
                return new List<object>();
            }
        }

        public async Task<object> GetVendorDetailsAsync(string vendorNo)
        {
            try
            {
                var company = _configuration["ApiSettings:Company"] ?? "KNQA";
                var response = await _httpClient.GetAsync($"Company('{company}')/Vendors('{vendorNo}')");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<object>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result;
                }
                else
                {
                    Console.WriteLine($"Error fetching vendor details: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetVendorDetailsAsync: {ex.Message}");
                return null;
            }
        }
    }

}
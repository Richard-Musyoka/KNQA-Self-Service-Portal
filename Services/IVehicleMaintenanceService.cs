// Services/IVehicleMaintenanceService.cs
using KNQASelfService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KNQASelfService.Services
{
    public interface IVehicleMaintenanceService
    {
        // Vehicle Maintenance CRUD
        Task<List<VehicleMaintenance>> GetAllMaintenanceRecordsAsync();
        Task<List<VehicleMaintenance>> GetMaintenanceByVehicleAsync(string vehicleNo);
        Task<List<VehicleMaintenance>> GetMaintenanceByDateRangeAsync(string fromDate, string toDate);
        Task<VehicleMaintenance> GetMaintenanceRecordAsync(string maintenanceNo);
        Task<string> CreateMaintenanceRecordAsync(VehicleMaintenanceCreate maintenance);
        Task<string> UpdateMaintenanceRecordAsync(VehicleMaintenance maintenance);
        Task<bool> DeleteMaintenanceRecordAsync(string maintenanceNo, string etag);
        Task<string> UpdateMaintenanceStatusAsync(string maintenanceNo, string status, string remarks = "");

        // Vehicle information
        Task<List<FleetVehicle>> GetFleetVehiclesAsync();
        Task<FleetVehicle> GetVehicleDetailsAsync(string vehicleNo);
        Task<List<FleetVehicle>> GetVehiclesDueForMaintenanceAsync();

        // Summary and reports
        Task<MaintenanceSummary> GetMaintenanceSummaryAsync();
        Task<MaintenanceSummary> GetVehicleMaintenanceSummaryAsync(string vehicleNo);
        Task<List<VehicleMaintenance>> GetUpcomingMaintenanceAsync(int days = 30);
        Task<List<VehicleMaintenance>> GetOverdueMaintenanceAsync();

        // Cost analysis
        Task<decimal> GetMaintenanceCostByVehicleAsync(string vehicleNo, string fromDate = "", string toDate = "");
        Task<decimal> GetMaintenanceCostByPeriodAsync(string period); // month, year, quarter
        Task<List<MaintenanceByVehicle>> GetMaintenanceCostAnalysisAsync();

        // Vendor management
        Task<List<object>> GetMaintenanceVendorsAsync();
        Task<object> GetVendorDetailsAsync(string vendorNo);
    }
}
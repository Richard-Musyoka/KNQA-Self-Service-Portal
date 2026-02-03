using KNQASelfService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KNQASelfService.Services
{
    public interface IFixedAssetService
    {
        // Existing methods
        Task<List<FixedAsset>> GetFixedAssetsAsync(FixedAssetFilter filter);
        Task<List<FixedAsset>> GetAssetsByEmployeeAsync(string employeeNo);
        Task<FixedAsset?> GetFixedAssetAsync(string assetNo);
        Task<FixedAssetSummary> GetAssetSummaryAsync(string? employeeNo = null);
        Task<string> UpdateAssetAllocationAsync(FixedAssetAllocation allocation, string etag);
        Task<List<string>> GetAssetClassesAsync();
        Task<List<string>> GetLocationsAsync();
        Task<List<string>> GetDepartmentsAsync();
        Task<List<AssetMaintenance>> GetAssetMaintenanceHistoryAsync(string assetNo);
        Task<string> AddMaintenanceRecordAsync(AssetMaintenance maintenance);
        Task<FixedAsset?> SearchByTagNoAsync(string tagNo);
        Task<FixedAsset?> SearchBySerialNoAsync(string serialNo);
        Task<List<FixedAsset>> GetAssetsDueForMaintenanceAsync(int daysAhead = 30);
        Task<byte[]> ExportAssetsToExcelAsync(FixedAssetFilter filter);
        Task<List<Employee>> GetEmployeesAsync();
        Task<List<Employee>> GetActiveEmployeesAsync();
        Task<Employee?> GetEmployeeAsync(string employeeNo);


        // NEW CRUD Methods
        Task<string> CreateFixedAssetAsync(FixedAsset asset);
        Task<string> UpdateFixedAssetAsync(FixedAsset asset);
        Task<string> DeleteFixedAssetAsync(string assetNo);
    }
}
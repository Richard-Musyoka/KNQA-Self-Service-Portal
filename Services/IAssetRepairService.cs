using KNQASelfService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KNQASelfService.Services
{
    public interface IAssetRepairService
    {
        // Basic CRUD operations
        Task<List<AssetRepair>> GetAssetRepairsAsync(AssetRepairFilter filter);
        Task<AssetRepair?> GetAssetRepairAsync(string maintenanceRefNo);
        Task<string> CreateAssetRepairAsync(AssetRepair repair);
        Task<string> UpdateAssetRepairAsync(AssetRepair repair);
        Task<string> DeleteAssetRepairAsync(string maintenanceRefNo);

        // Employee-specific operations
        Task<List<AssetRepair>> GetRepairsByEmployeeAsync(string employeeNo);
        Task<List<AssetRepair>> GetMyOpenRepairsAsync(string employeeNo);

        // Asset-related operations
        Task<List<AssetRepair>> GetRepairsByAssetAsync(string assetNo);

        // Summary and reporting
        Task<AssetRepairSummary> GetRepairSummaryAsync(string? employeeNo = null);
        Task<List<string>> GetRepairStatusListAsync();
        Task<List<string>> GetRepairPriorityListAsync();

        // Dashboard operations
        Task<int> GetOpenRepairsCountAsync();
        Task<int> GetCriticalRepairsCountAsync();
        Task<List<AssetRepair>> GetRecentRepairsAsync(int count = 10);

        // Assignment and workflow
        Task<string> AssignRepairAsync(string maintenanceRefNo, string technician);
        Task<string> UpdateRepairStatusAsync(string maintenanceRefNo, string status, string? resolution = null);

        // Export operations
        Task<byte[]> ExportRepairsToExcelAsync(AssetRepairFilter filter);
    }
}
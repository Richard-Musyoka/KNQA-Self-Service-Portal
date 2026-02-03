using KNQASelfService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KNQASelfService.Services
{
    public interface IPerformanceTargetService
    {
        // Basic CRUD operations
        Task<List<PerformanceTarget>> GetPerformanceTargetsAsync(PerformanceTargetFilter filter);
        Task<PerformanceTarget?> GetPerformanceTargetAsync(string objectiveNo);
        Task<string> CreatePerformanceTargetAsync(PerformanceTarget target);
        Task<string> UpdatePerformanceTargetAsync(PerformanceTarget target);
        Task<string> DeletePerformanceTargetAsync(string objectiveNo);
        Task<string> SubmitForApprovalAsync(string objectiveNo);
        Task<string> ApproveTargetAsync(string objectiveNo, string approverComments = "");
        Task<string> RejectTargetAsync(string objectiveNo, string rejectionReason = "");

        // Lookup data
        Task<List<AppraisalCategory>> GetAppraisalCategoriesAsync();
        Task<List<PerformanceCategory>> GetPerformanceCategoriesAsync();
        Task<List<string>> GetAppraisalPeriodsAsync();
        Task<List<string>> GetAppraisalStatusListAsync();

        // Employee-specific operations
        Task<List<PerformanceTarget>> GetTargetsByAppraiseeAsync(string employeeNo);
        Task<List<PerformanceTarget>> GetTargetsByAppraiserAsync(string appraiserNo);
        Task<List<PerformanceTarget>> GetMyDraftTargetsAsync(string employeeNo);
        Task<List<PerformanceTarget>> GetMyPendingApprovalTargetsAsync(string employeeNo);
        Task<List<PerformanceTarget>> GetTargetsPendingMyApprovalAsync(string appraiserNo);

        // ADDED: GetPerformanceTargetsByEmployeeAsync (synonym for GetTargetsByAppraiseeAsync)
        Task<List<PerformanceTarget>> GetPerformanceTargetsByEmployeeAsync(string employeeNo);

        Task<Employee?> GetEmployeeAsync(string employeeNo);
        Task<List<Employee>> GetEmployeesByDepartmentAsync(string departmentCode);

        // Summary and reporting
        Task<PerformanceTargetSummary> GetTargetSummaryAsync(string? employeeNo = null);

        // Helper methods
        Task<string> GenerateObjectiveNo();
        Task<List<Employee>> GetPotentialAppraisersAsync(string employeeNo);

        // Export operations
        Task<byte[]> ExportTargetsToExcelAsync(PerformanceTargetFilter filter);
        Task<byte[]> ExportTargetToPdfAsync(string objectiveNo);
    }
}
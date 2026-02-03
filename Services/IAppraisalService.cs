using KNQASelfService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KNQASelfService.Services
{
    public interface IAppraisalService
    {
        // Basic CRUD operations
        Task<List<Appraisal>> GetAppraisalsAsync(AppraisalFilter filter);
        Task<Appraisal?> GetAppraisalAsync(string appraisalNo);
        Task<string> CreateAppraisalAsync(Appraisal appraisal);
        Task<string> UpdateAppraisalAsync(Appraisal appraisal);
        Task<string> DeleteAppraisalAsync(string appraisalNo);

        // Status transition operations
        Task<string> SubmitAppraisalAsync(string appraisalNo, string employeeComments = "");

        // NEW: Added missing methods
        Task<string> StartAppraisalAsync(string appraisalNo);
        Task<string> StartAgreementAsync(string appraisalNo);

        // Existing methods
        Task<string> AppraiseAsync(string appraisalNo, List<AppraisalLine> appraisalLines, string appraiserComments = "");
        Task<string> AgreeOnAppraisalAsync(string appraisalNo, List<AppraisalLine> agreedLines, string agreedComments = "");
        Task<string> CompleteAppraisalAsync(string appraisalNo);

        // Employee-specific operations
        Task<List<Appraisal>> GetAppraisalsByEmployeeAsync(string employeeNo);
        Task<List<Appraisal>> GetAppraisalsByAppraiserAsync(string appraiserNo);
        Task<List<Appraisal>> GetMyPendingAppraisalsAsync(string employeeNo);
        Task<List<Appraisal>> GetAppraisalsPendingMyApprovalAsync(string appraiserNo);

        // Sync operations
        Task<string> SyncAppraisalFromNavAsync(string appraisalNo);

        // Target linking operations
        Task<Appraisal?> CreateAppraisalFromTargetAsync(string performanceTargetNo);
        Task<List<Appraisal>> GetAppraisalsLinkedToTargetAsync(string performanceTargetNo);
        Task<bool> LinkAppraisalToTargetAsync(string appraisalNo, string performanceTargetNo);

        // Summary and reporting
        Task<AppraisalSummary> GetAppraisalSummaryAsync(string? employeeNo = null, string? appraiserNo = null);
        Task<decimal> CalculateOverallRatingAsync(string appraisalNo);
        Task<string> DeterminePerformanceCategoryAsync(decimal rating);

        // Lookup data
        Task<List<string>> GetAppraisalTypesAsync();
        Task<List<string>> GetPerformanceEvaluationsAsync();

        // Helper methods
        Task<string> GenerateAppraisalNo();
        Task<List<PerformanceTarget>> GetEligibleTargetsForAppraisalAsync(string employeeNo);

        // Export operations
        Task<byte[]> ExportAppraisalToExcelAsync(string appraisalNo);
        Task<byte[]> ExportAppraisalToPdfAsync(string appraisalNo);
        Task<byte[]> ExportAppraisalSummaryReportAsync(AppraisalFilter filter);
    }
}
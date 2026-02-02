using KNQASelfService.Models;

public interface ILeavePlanService
{
    Task<List<LeavePlan>> GetLeavePlansByEmployeeAsync(string employeeNo);
    Task<LeavePlan> GetLeavePlanAsync(string applicationNo);
    Task<LeavePlanSummary> GetLeavePlanSummaryAsync(string employeeNo);

    // Create method
    Task<(bool Success, string Message, LeavePlan Data)> CreateLeavePlanAsync(LeavePlanCreate model);

    // Update method using LeavePlanUpdate model
    Task<(bool Success, string Message)> UpdateLeavePlanAsync(LeavePlanUpdate model, string etag);

    // Delete method - note: your service returns bool, not tuple
    Task<bool> DeleteLeavePlanAsync(string applicationNo, string etag);

    // Additional methods (if needed)
    Task<List<LeavePlan>> GetLeavePlansAsync(LeavePlanFilter filter);
    Task<List<LeavePlan>> GetLeavePlansByStatusAsync(string employeeNo, string status);
    Task<(bool Success, string Message)> UpdateLeavePlanStatusAsync(string applicationNo, string newStatus, string etag);
    Task<List<string>> ValidateLeavePlanAsync(LeavePlanCreate leavePlan);
    Task<bool> LeavePlanExistsAsync(string applicationNo);
}
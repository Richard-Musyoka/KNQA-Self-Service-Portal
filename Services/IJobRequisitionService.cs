// IJobRequisitionService.cs
using KNQASelfService.Models;

namespace KNQASelfService.Services
{
    public interface IJobRequisitionService
    {
        // READ operations
        Task<List<JobRequisition>> GetJobRequisitionsByEmployeeAsync(string employeeNo);
        Task<JobRequisition> GetJobRequisitionAsync(string applicationNo);
        Task<List<JobRequisition>> GetJobRequisitionsAsync(JobRequisitionFilter filter);
        Task<List<JobRequisition>> GetJobRequisitionsByStatusAsync(string status);
        Task<JobRequisitionSummary> GetJobRequisitionSummaryAsync();

        // CREATE operations
        Task<(bool Success, string Message, JobRequisition Data)> CreateJobRequisitionAsync(JobRequisitionCreate model);

        // UPDATE operations
        Task<(bool Success, string Message)> UpdateJobRequisitionAsync(JobRequisitionUpdate model, string etag);

        // DELETE operations
        Task<bool> DeleteJobRequisitionAsync(string applicationNo, string etag);

        // Additional operations
        Task<(bool Success, string Message)> UpdateJobRequisitionStatusAsync(string applicationNo, string newStatus, string etag);
        Task<List<string>> ValidateJobRequisitionAsync(JobRequisitionCreate model);
        Task<bool> JobRequisitionExistsAsync(string applicationNo);
        Task<bool> PostJobRequisitionAsync(string applicationNo, string etag);
    }
}
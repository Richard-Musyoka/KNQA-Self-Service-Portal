// IIncidentManagementService.cs
using KNQASelfService.Models;

namespace KNQASelfService.Services
{
    public interface IIncidentManagementService
    {
        // READ operations
        Task<List<IncidentManagement>> GetIncidentsByEmployeeAsync(string employeeNo);
        Task<IncidentManagement> GetIncidentAsync(string incidentReference);
        Task<List<IncidentManagement>> GetIncidentsAsync(IncidentManagementFilter filter);
        Task<List<IncidentManagement>> GetIncidentsByStatusAsync(string employeeNo, string status);
        Task<IncidentManagementSummary> GetIncidentSummaryAsync(string employeeNo);

        // CREATE operations
        Task<(bool Success, string Message, IncidentManagement Data)> CreateIncidentAsync(IncidentManagementCreate model);

        // UPDATE operations
        Task<(bool Success, string Message)> UpdateIncidentAsync(IncidentManagementUpdate model, string etag);

        // DELETE operations
        Task<bool> DeleteIncidentAsync(string incidentReference, string etag);

        // Additional operations
        Task<List<string>> ValidateIncidentAsync(IncidentManagementCreate model);
        Task<bool> IncidentExistsAsync(string incidentReference);
    }
}
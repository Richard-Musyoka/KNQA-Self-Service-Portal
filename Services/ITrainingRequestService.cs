// ITrainingRequestService.cs
using KNQASelfService.Models;

namespace KNQASelfService.Services
{
    public interface ITrainingRequestService
    {
        // READ operations
        Task<List<TrainingRequest>> GetTrainingRequestsByEmployeeAsync(string employeeNo);
        Task<TrainingRequest> GetTrainingRequestAsync(string requestNo);
        Task<List<TrainingRequest>> GetTrainingRequestsAsync(TrainingRequestFilter filter);
        Task<List<TrainingRequest>> GetTrainingRequestsByStatusAsync(string employeeNo, string status);
        Task<TrainingRequestSummary> GetTrainingRequestSummaryAsync(string employeeNo);

        // CREATE operations
        Task<(bool Success, string Message, TrainingRequest Data)> CreateTrainingRequestAsync(TrainingRequestCreate model);

        // UPDATE operations
        Task<(bool Success, string Message)> UpdateTrainingRequestAsync(TrainingRequestUpdate model, string etag);

        // DELETE operations
        Task<bool> DeleteTrainingRequestAsync(string requestNo, string etag);

        // Additional operations
        Task<(bool Success, string Message)> UpdateTrainingRequestStatusAsync(string requestNo, string newStatus, string etag);
        Task<List<string>> ValidateTrainingRequestAsync(TrainingRequestCreate model);
        Task<bool> TrainingRequestExistsAsync(string requestNo);
        Task<bool> ConvertToTrainingPlanAsync(string requestNo, string etag);
    }
}
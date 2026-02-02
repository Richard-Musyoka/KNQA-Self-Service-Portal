// ITrainingEvaluationService.cs
using KNQASelfService.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace KNQASelfService.Services
{
    public interface ITrainingEvaluationService
    {
        // READ operations
        Task<List<TrainingEvaluation>> GetTrainingEvaluationsByEmployeeAsync(string employeeNo);
        Task<TrainingEvaluation> GetTrainingEvaluationAsync(string evaluationNo);
        Task<List<TrainingEvaluation>> GetTrainingEvaluationsAsync(TrainingEvaluationFilter filter);
        Task<List<TrainingEvaluation>> GetEvaluationsByTrainingCodeAsync(string trainingCode);
        Task<TrainingEvaluationSummary> GetTrainingEvaluationSummaryAsync(string employeeNo);

        // CREATE operations
        Task<(bool Success, string Message, TrainingEvaluation Data)> CreateTrainingEvaluationAsync(TrainingEvaluationCreate model);

        // UPDATE operations
        Task<(bool Success, string Message)> UpdateTrainingEvaluationAsync(TrainingEvaluationUpdate model, string etag);

        // DELETE operations
        Task<bool> DeleteTrainingEvaluationAsync(string evaluationNo, string etag);

        // Additional operations
        Task<(bool Success, string Message)> UpdateTrainingEvaluationStatusAsync(string evaluationNo, string newStatus, string etag);
        Task<List<string>> ValidateTrainingEvaluationAsync(TrainingEvaluationCreate model);
        Task<bool> TrainingEvaluationExistsAsync(string evaluationNo);

        // File upload operations
        Task<FileUploadResponse> UploadCertificateAsync(string evaluationNo, IBrowserFile file);
        Task<bool> DeleteCertificateAsync(string evaluationNo, string fileName);
        Task<byte[]> DownloadCertificateAsync(string fileUrl);
    }
}
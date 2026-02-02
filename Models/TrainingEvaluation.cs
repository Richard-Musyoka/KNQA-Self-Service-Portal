// TrainingEvaluation.cs
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;

namespace KNQASelfService.Models
{
    public class TrainingEvaluation
    {
        public string EvaluationNo { get; set; } // e.g., "TREV00003"
        public string EvaluationDate { get; set; } // "11/17/2025"
        public string EmployeeNo { get; set; } // "KNQA/0019/2021"
        public string EmployeeName { get; set; } // "Recho Chepkurui MUTHURI"
        public string DepartmentCode { get; set; }
        public string Designation { get; set; }
        public string TrainingCode { get; set; } // "TRQ0005"
        public string PlannedStartDate { get; set; } // "11/17/2025"
        public string PlannedEndDate { get; set; } // "11/20/2025"
        public decimal NoOfDays { get; set; } // 3.00
        public string CourseTitle { get; set; } // "MANAGEMENT"
        public string Description { get; set; }
        public string Venue { get; set; }
        public string Status { get; set; } // "Open"
        public int NoOfApprovals { get; set; }

        // Evaluation Questions
        public int OverallRating { get; set; } // 1-5 scale
        public string TrainerEffectiveness { get; set; } // Excellent, Good, Fair, Poor
        public string ContentRelevance { get; set; } // Very Relevant, Relevant, Somewhat, Not Relevant
        public string TrainingMaterials { get; set; } // Excellent, Good, Fair, Poor
        public string TrainingFacilities { get; set; } // Excellent, Good, Fair, Poor
        public string TrainingDuration { get; set; } // Too Long, Just Right, Too Short
        public bool MetObjectives { get; set; }
        public bool WouldRecommend { get; set; }
        public string WhatLikedMost { get; set; }
        public string WhatCouldImprove { get; set; }
        public string AdditionalComments { get; set; }

        // Certification
        public bool CertificateReceived { get; set; }
        public string CertificateFileName { get; set; }
        public string CertificateFileUrl { get; set; }
        public long CertificateFileSize { get; set; }
        public string CertificateUploadDate { get; set; }

        // Skills Assessment
        public int SkillImprovementRating { get; set; } // 1-5 scale
        public string ApplicableToWork { get; set; } // Yes, Partially, No
        public string ImplementationPlan { get; set; }
        public string ExpectedImpact { get; set; }

        public string ODataEtag { get; set; }

        // Additional fields
        public string TrainerName { get; set; }
        public string TrainingProvider { get; set; }
        public decimal TrainingCost { get; set; }
        public string TrainingType { get; set; }
    }

    public class TrainingEvaluationCreate
    {
        public string TrainingCode { get; set; }
        public int OverallRating { get; set; }
        public string TrainerEffectiveness { get; set; }
        public string ContentRelevance { get; set; }
        public string TrainingMaterials { get; set; }
        public string TrainingFacilities { get; set; }
        public string TrainingDuration { get; set; }
        public bool MetObjectives { get; set; }
        public bool WouldRecommend { get; set; }
        public string WhatLikedMost { get; set; }
        public string WhatCouldImprove { get; set; }
        public string AdditionalComments { get; set; }

        // Skills Assessment
        public int SkillImprovementRating { get; set; }
        public string ApplicableToWork { get; set; }
        public string ImplementationPlan { get; set; }
        public string ExpectedImpact { get; set; }

        // Certificate Upload
        public bool CertificateReceived { get; set; }
        public IBrowserFile CertificateFile { get; set; }
    }

    public class TrainingEvaluationUpdate
    {
        public string EvaluationNo { get; set; }
        public int OverallRating { get; set; }
        public string TrainerEffectiveness { get; set; }
        public string ContentRelevance { get; set; }
        public string TrainingMaterials { get; set; }
        public string TrainingFacilities { get; set; }
        public string TrainingDuration { get; set; }
        public bool MetObjectives { get; set; }
        public bool WouldRecommend { get; set; }
        public string WhatLikedMost { get; set; }
        public string WhatCouldImprove { get; set; }
        public string AdditionalComments { get; set; }
        public string Status { get; set; }

        // Skills Assessment
        public int SkillImprovementRating { get; set; }
        public string ApplicableToWork { get; set; }
        public string ImplementationPlan { get; set; }
        public string ExpectedImpact { get; set; }

        // Certificate
        public bool CertificateReceived { get; set; }
        public string CertificateFileName { get; set; }
        public string CertificateFileUrl { get; set; }
        public long CertificateFileSize { get; set; }
    }

    public class TrainingEvaluationFilter
    {
        public string EmployeeNo { get; set; }
        public string DepartmentCode { get; set; }
        public string Status { get; set; }
        public string SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string TrainingType { get; set; }
        public string OrderBy { get; set; } = "Evaluation_Date desc";
    }

    public class TrainingEvaluationSummary
    {
        public int TotalEvaluations { get; set; }
        public int PendingEvaluations { get; set; }
        public int SubmittedEvaluations { get; set; }
        public int ApprovedEvaluations { get; set; }
        public decimal AverageRating { get; set; }
        public int WithCertificates { get; set; }
        public int WouldRecommendCount { get; set; }
        public int MetObjectivesCount { get; set; }
    }

    public class TrainingEvaluationODataResponse
    {
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; }

        [JsonProperty("value")]
        public List<TrainingEvaluation> Value { get; set; }
    }

    // For file upload
    public class FileUploadResponse
    {
        public bool Success { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public long FileSize { get; set; }
        public string Message { get; set; }
    }
}
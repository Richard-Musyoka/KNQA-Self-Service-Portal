using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace KNQASelfService.Models
{
    public class Appraisal
    {
        [JsonProperty("Appraisal_No")]
        public string AppraisalNo { get; set; } = string.Empty;

        [JsonProperty("Employee_No")]
        public string EmployeeNo { get; set; } = string.Empty;

        public DateTime? AppraisalStartDate { get; set; }
        public DateTime? AgreementStartDate { get; set; }


        [JsonProperty("Appraisee_ID")]
        public string AppraiseeID { get; set; } = string.Empty;

        [JsonProperty("Appraisee_Name")]
        public string AppraiseeName { get; set; } = string.Empty;

        [JsonProperty("Appraisee_Job_ID")]
        public string AppraiseeJobID { get; set; } = string.Empty;

        [JsonProperty("Appraisee_Job_Title")]
        public string AppraiseeJobTitle { get; set; } = string.Empty;

        [JsonProperty("Department_Code")]
        public string DepartmentCode { get; set; } = string.Empty;

        [JsonProperty("Department_Name")]
        public string DepartmentName { get; set; } = string.Empty;

        [JsonProperty("Appraiser_No")]
        public string AppraiserNo { get; set; } = string.Empty;

        [JsonProperty("Appraiser_Name")]
        public string AppraiserName { get; set; } = string.Empty;

        [JsonProperty("Appraiser_Job_Title")]
        public string AppraiserJobTitle { get; set; } = string.Empty;

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("Total_Score_Employee")]
        public decimal TotalScoreEmployee { get; set; }

        [JsonProperty("Total_Score_Supervisor")]
        public decimal TotalScoreSupervisor { get; set; }

        [JsonProperty("Total_Score_Agreed")]
        public decimal TotalScoreAgreed { get; set; }

        [JsonProperty("Total_Maximum_Score")]
        public decimal TotalMaximumScore { get; set; } = 100.00m;

        [JsonProperty("Appraisal_Period")]
        public string AppraisalPeriod { get; set; } = string.Empty;

        [JsonProperty("Effective_Date")]
        public DateTime? EffectiveDate { get; set; }

        [JsonProperty("Appraisal_Type")]
        public string AppraisalType { get; set; } = string.Empty;

        [JsonProperty("Performance_Evaluation")]
        public string PerformanceEvaluation { get; set; } = string.Empty;

        [JsonProperty("Performance_Category")]
        public string PerformanceCategory { get; set; } = string.Empty;

        [JsonProperty("Overall_Rating")]
        public decimal? OverallRating { get; set; }

        [JsonProperty("Employee_Comments")]
        public string EmployeeComments { get; set; } = string.Empty;

        [JsonProperty("Appraiser_Comments")]
        public string AppraiserComments { get; set; } = string.Empty;

        [JsonProperty("Agreed_Comments")]
        public string AgreedComments { get; set; } = string.Empty;

        [JsonProperty("Created_Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [JsonProperty("Submitted_Date")]
        public DateTime? SubmittedDate { get; set; }

        [JsonProperty("Appraised_Date")]
        public DateTime? AppraisedDate { get; set; }

        [JsonProperty("Agreed_Date")]
        public DateTime? AgreedDate { get; set; }

        [JsonProperty("Completed_Date")]
        public DateTime? CompletedDate { get; set; }

        // Navigation properties
        [JsonIgnore]
        public List<AppraisalLine> AppraisalLines { get; set; } = new List<AppraisalLine>();

        [JsonIgnore]
        public PerformanceTarget? LinkedPerformanceTarget { get; set; }

        [JsonIgnore]
        public Employee? Appraisee { get; set; }

        [JsonIgnore]
        public Employee? Appraiser { get; set; }

        // Helper properties
        [JsonIgnore]
        public decimal CompletionPercentage => TotalMaximumScore > 0 ? (TotalScoreAgreed / TotalMaximumScore) * 100 : 0;

        [JsonIgnore]
        public string StatusDisplay => Status switch
        {
            AppraisalStatus.OPEN => "Open",
            AppraisalStatus.IN_PROGRESS => "In Progress",
            AppraisalStatus.SUBMITTED => "Submitted",
            AppraisalStatus.APPRAISED => "Appraised",
            AppraisalStatus.AGREED => "Agreed",
            AppraisalStatus.COMPLETED => "Completed",
            _ => Status
        };

        [JsonIgnore]
        public bool CanEdit => Status == AppraisalStatus.OPEN || Status == AppraisalStatus.IN_PROGRESS;

        [JsonIgnore]
        public bool CanSubmit => Status == AppraisalStatus.OPEN || Status == AppraisalStatus.IN_PROGRESS;

        [JsonIgnore]
        public bool CanAppraise => Status == AppraisalStatus.SUBMITTED && !string.IsNullOrEmpty(AppraiserNo);

        [JsonIgnore]
        public bool CanAgree => Status == AppraisalStatus.APPRAISED;

        [JsonIgnore]
        public bool IsCompleted => Status == AppraisalStatus.COMPLETED || Status == AppraisalStatus.AGREED;
    }

    public class AppraisalLine
    {
        [JsonProperty("Line_No")]
        public int LineNo { get; set; }

        [JsonProperty("Appraisal_No")]
        public string AppraisalNo { get; set; } = string.Empty;

        [JsonProperty("Performance_Target_No")]
        public string PerformanceTargetNo { get; set; } = string.Empty;

        [JsonProperty("Key_Performance_Area")]
        public string KeyPerformanceArea { get; set; } = string.Empty;

        [JsonProperty("Key_Performance_Indicator")]
        public string KeyPerformanceIndicator { get; set; } = string.Empty;

        [JsonProperty("Performance_Measure")]
        public string PerformanceMeasure { get; set; } = string.Empty;

        [JsonProperty("Target")]
        public string Target { get; set; } = string.Empty;

        [JsonProperty("Maximum_Weighting")]
        public decimal MaximumWeighting { get; set; }

        [JsonProperty("Employee_Score")]
        public decimal EmployeeScore { get; set; }

        [JsonProperty("Supervisor_Score")]
        public decimal SupervisorScore { get; set; }

        [JsonProperty("Agreed_Score")]
        public decimal AgreedScore { get; set; }

        [JsonProperty("Progress_Assessment_Remarks")]
        public string ProgressAssessmentRemarks { get; set; } = string.Empty;

        [JsonProperty("Employee_Remarks")]
        public string EmployeeRemarks { get; set; } = string.Empty;

        [JsonProperty("Supervisor_Remarks")]
        public string SupervisorRemarks { get; set; } = string.Empty;

        [JsonProperty("Agreed_Remarks")]
        public string AgreedRemarks { get; set; } = string.Empty;

        // Navigation property
        [JsonIgnore]
        public TargetLine? LinkedTargetLine { get; set; }

        // Helper properties
        [JsonIgnore]
        public decimal EmployeeWeightedScore => MaximumWeighting > 0 ? (EmployeeScore / 100) * MaximumWeighting : 0;

        [JsonIgnore]
        public decimal SupervisorWeightedScore => MaximumWeighting > 0 ? (SupervisorScore / 100) * MaximumWeighting : 0;

        [JsonIgnore]
        public decimal AgreedWeightedScore => MaximumWeighting > 0 ? (AgreedScore / 100) * MaximumWeighting : 0;

        [JsonIgnore]
        public decimal ScoreDifference => Math.Abs(EmployeeScore - SupervisorScore);

        [JsonIgnore]
        public bool HasDisagreement => ScoreDifference >= 10; // 10% difference threshold
    }

    public static class AppraisalType
    {
        public const string MID_YEAR = "MID-YEAR";
        public const string ANNUAL = "ANNUAL";
        public const string PROBATION = "PROBATION";
        public const string PROMOTION = "PROMOTION";
        public const string SPECIAL = "SPECIAL";
    }

    public static class PerformanceEvaluation
    {
        public const string EXCELLENT = "EXCELLENT";
        public const string VERY_GOOD = "VERY GOOD";
        public const string GOOD = "GOOD";
        public const string FAIR = "FAIR";
        public const string POOR = "POOR";
    }
    public static class AppraisalStatus
    {
        public const string OPEN = "OPEN";
        public const string IN_PROGRESS = "IN_PROGRESS";
        public const string DRAFT = "DRAFT";
        public const string UNDER_REVIEW = "UNDER_REVIEW";
        public const string REJECTED = "REJECTED";
        public const string PENDING_APPROVAL = "PENDING_APPROVAL";
        public const string SUBMITTED = "SUBMITTED";
        public const string APPRAISED = "APPRAISED";
        public const string AGREED = "AGREED";
        public const string COMPLETED = "COMPLETED";
        public const string APPROVED = "APPROVED";
        public const string APPRAISAL_IN_PROGRESS = "APPRAISAL_IN_PROGRESS";
        public const string AGREEMENT_IN_PROGRESS = "AGREEMENT_IN_PROGRESS"; 

        // You can also add helper methods if needed
        public static List<string> GetAllStatuses()
        {
            return new List<string>
        {
            OPEN,
            IN_PROGRESS,
            SUBMITTED,
            APPRAISAL_IN_PROGRESS,
            AGREEMENT_IN_PROGRESS,
            APPRAISED,
            APPROVED,
            AGREED,
            COMPLETED
        };
        }

        public static string GetDisplayText(string status)
        {
            return status switch
            {
                OPEN => "Open",
                IN_PROGRESS => "In Progress",
                SUBMITTED => "Submitted",
                APPRAISAL_IN_PROGRESS=> "APPRAISAL_IN_PROGRESS",
                AGREEMENT_IN_PROGRESS=> "AGREEMENT_IN_PROGRESS",
                APPROVED =>"Approved",
                APPRAISED => "Appraised",
                AGREED => "Agreed",
                COMPLETED => "Completed",
                _ => status
            };
        }
    }
    public class AppraisalFilter
    {
        public string? EmployeeNo { get; set; }
        public string? AppraiserNo { get; set; }
        public string? Status { get; set; }
        public string? AppraisalPeriod { get; set; }
        public string? AppraisalType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class AppraisalSummary
    {
        public int TotalAppraisals { get; set; }
        public int OpenAppraisals { get; set; }
        public int InProgressAppraisals { get; set; }
        public int SubmittedAppraisals { get; set; }
        public int AppraisedAppraisals { get; set; }
        public int AgreedAppraisals { get; set; }
        public int CompletedAppraisals { get; set; }
        public int MyAppraisals { get; set; }
        public int AppraisalsForMyApproval { get; set; }
    }
}
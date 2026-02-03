using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace KNQASelfService.Models
{
   
        public class PerformanceTarget
        {
            [JsonProperty("Objective_No")]
            public string? ObjectiveNo { get; set; }

            [JsonProperty("Employee_No")]
            [Required(ErrorMessage = "Appraisee Employee No is required")]
            public string? EmployeeNo { get; set; }

            [JsonProperty("Appraisee_Name")]
            public string? AppraiseeName { get; set; }

            [JsonProperty("Appraisee_ID")]
            public string? AppraiseeID { get; set; }

            [JsonProperty("Appraisee_Job_ID")]
            public string? AppraiseeJobID { get; set; }

            [JsonProperty("Appraisee_Job_Title")]
            public string? AppraiseeJobTitle { get; set; }

            [JsonProperty("Department_Code")]
            public string? DepartmentCode { get; set; }

            [JsonProperty("Department_Name")]
            public string? DepartmentName { get; set; }

            [JsonProperty("Appraiser_No")]
            [Required(ErrorMessage = "Appraiser Employee No is required")]
            public string? AppraiserNo { get; set; }

            [JsonProperty("Appraiser_Name")]
            public string? AppraiserName { get; set; }

            [JsonProperty("Appraiser_Job_Title")]
            public string? AppraiserJobTitle { get; set; }

            [JsonProperty("Status")]
            public string? Status { get; set; } = "Pending Approval";

            [JsonProperty("Appraisal_Category")]
            [Required(ErrorMessage = "Appraisal Category is required")]
            public string? AppraisalCategory { get; set; }

            [JsonProperty("Appraisal_Period")]
            [Required(ErrorMessage = "Appraisal Period is required")]
            public string? AppraisalPeriod { get; set; }

            [JsonProperty("Agreed_Performance_Category")]
            [Required(ErrorMessage = "Agreed Performance Category is required")]
            public string? AgreedPerformanceCategory { get; set; }

            [JsonProperty("Approved")]
            public bool Approved { get; set; } = false;

            [JsonProperty("Created_Date")]
            public DateTime? CreatedDate { get; set; } = DateTime.Now;

            [JsonProperty("Submitted_Date")]
            public DateTime? SubmittedDate { get; set; }

            [JsonProperty("Approved_Date")]
            public DateTime? ApprovedDate { get; set; }

            [JsonProperty("Remarks")]
            public string? Remarks { get; set; }

            [JsonProperty("@odata.etag")]
            public string? ODataEtag { get; set; }

            // Target lines collection (these might be in a separate table)
            public List<TargetLine> TargetLines { get; set; } = new List<TargetLine>();
        }

        // Add TargetLine model for BC structure
        public class TargetLine
        {
            [JsonProperty("Objective_No")]
            public string? ObjectiveNo { get; set; }

            [JsonProperty("Line_No")]
            public int LineNo { get; set; }

            [JsonProperty("Key_Performance_Area")]
            public string? KeyPerformanceArea { get; set; }

            [JsonProperty("Key_Performance_Indicator")]
            public string? KeyPerformanceIndicator { get; set; }

            [JsonProperty("Performance_Measure")]
            public string? PerformanceMeasure { get; set; }

            [JsonProperty("Target")]
            public string? Target { get; set; }

            [JsonProperty("Weighting")]
            public decimal Weighting { get; set; }

            [JsonProperty("Timeline")]
            public string? Timeline { get; set; }

            [JsonProperty("Resources_Required")]
            public string? ResourcesRequired { get; set; }

            [JsonProperty("Success_Criteria")]
            public string? SuccessCriteria { get; set; }

            [JsonProperty("Remarks")]
            public string? Remarks { get; set; }
        }
    
    public class PerformanceTargetFilter
    {
        public string? SearchTerm { get; set; }
        public string? EmployeeNo { get; set; }
        public string? AppraiserNo { get; set; }
        public string? Status { get; set; }
        public string? AppraisalCategory { get; set; }
        public string? AppraisalPeriod { get; set; }
        public string? DepartmentCode { get; set; }
        public string? Period { get; set; }
        public string? OrderBy { get; set; } = "CreatedDate desc";
    }

    public class PerformanceTargetSummary
    {
        public int TotalTargets { get; set; }
        public int DraftTargets { get; set; }
        public int PendingApproval { get; set; }
        public int ApprovedTargets { get; set; }
        public int RejectedTargets { get; set; }
        public int MyPendingApproval { get; set; }
        public int MyDraftTargets { get; set; }
    }

    public class AppraisalCategory
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
        public bool Active { get; set; }
    }

    public class PerformanceCategory
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
        public decimal? MinimumScore { get; set; }
        public decimal? MaximumScore { get; set; }
    }



    public static class AppraisalCategories
    {
        public const string TARGET_SETTING = "TARGETSET";
        public const string MID_YEAR_REVIEW = "MIDYEAR";
        public const string ANNUAL_REVIEW = "ANNUAL";
        public const string PROBATION_REVIEW = "PROBATION";
        public const string PROMOTION_REVIEW = "PROMOTION";
    }
}
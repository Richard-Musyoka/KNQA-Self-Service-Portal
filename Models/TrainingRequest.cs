// TrainingRequest.cs
using Newtonsoft.Json;

namespace KNQASelfService.Models
{
    public class TrainingRequest
    {
        public string RequestNo { get; set; } // e.g., "TRQ0004"
        public string RequestDate { get; set; } // "11/17/2025"
        public string EmployeeNo { get; set; } // "1990171964"
        public string RequestedBy { get; set; } // "THADEUS OANYA"
        public string JobPosition { get; set; }
        public string DepartmentCode { get; set; } // "ADM"
        public string DepartmentName { get; set; } // "Administration"
        public string Status { get; set; } // "Released"
        public bool ConvertedToPlan { get; set; }
        public string HighestAcademicQualification { get; set; } // "Certificate"
        public bool CurrentlyPursuingTraining { get; set; }
        public string TrainingInstitution { get; set; } // "promate venture"
        public string CourseTitle { get; set; } // "Cmmunication"
        public string SponsoringBody { get; set; }
        public bool SelfFunded { get; set; }
        public string StartDate { get; set; } // "11/17/2025"
        public string EndDate { get; set; }
        public string ODataEtag { get; set; }

        // Additional fields for display/processing
        public string CourseDuration { get; set; }
        public string TrainingType { get; set; }
        public string TrainingMode { get; set; } // Online, In-person, Hybrid
        public decimal EstimatedCost { get; set; }
        public string Justification { get; set; }
        public string ExpectedOutcomes { get; set; }
        public string ApprovalComments { get; set; }
        public string ApprovedBy { get; set; }
        public string ApprovalDate { get; set; }
    }

    public class TrainingRequestCreate
    {
        public string EmployeeNo { get; set; }
        public string JobPosition { get; set; }
        public string DepartmentCode { get; set; }
        public string HighestAcademicQualification { get; set; }
        public bool CurrentlyPursuingTraining { get; set; }
        public string TrainingInstitution { get; set; }
        public string CourseTitle { get; set; }
        public string SponsoringBody { get; set; }
        public bool SelfFunded { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string CourseDuration { get; set; }
        public string TrainingType { get; set; }
        public string TrainingMode { get; set; }
        public decimal EstimatedCost { get; set; }
        public string Justification { get; set; }
        public string ExpectedOutcomes { get; set; }
    }

    public class TrainingRequestUpdate
    {
        public string RequestNo { get; set; }
        public string HighestAcademicQualification { get; set; }
        public bool CurrentlyPursuingTraining { get; set; }
        public string TrainingInstitution { get; set; }
        public string CourseTitle { get; set; }
        public string SponsoringBody { get; set; }
        public bool SelfFunded { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string CourseDuration { get; set; }
        public string TrainingType { get; set; }
        public string TrainingMode { get; set; }
        public decimal EstimatedCost { get; set; }
        public string Justification { get; set; }
        public string ExpectedOutcomes { get; set; }
        public string Status { get; set; }
        public bool ConvertedToPlan { get; set; }
    }

    public class TrainingRequestFilter
    {
        public string EmployeeNo { get; set; }
        public string DepartmentCode { get; set; }
        public string Status { get; set; }
        public string SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string TrainingType { get; set; }
        public string OrderBy { get; set; } = "Request_Date desc";
    }

    public class TrainingRequestSummary
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int SelfFundedRequests { get; set; }
        public int OrganizationFundedRequests { get; set; }
        public decimal TotalEstimatedCost { get; set; }
    }

    public class TrainingRequestODataResponse
    {
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; }

        [JsonProperty("value")]
        public List<TrainingRequest> Value { get; set; }
    }
}
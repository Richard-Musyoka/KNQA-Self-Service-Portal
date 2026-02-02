// JobRequisition.cs
using Newtonsoft.Json;

namespace KNQASelfService.Models
{
    public class JobRequisition
    {
        public string ApplicationNo { get; set; } // e.g., "RN00004"
        public string DocumentDate { get; set; } // "11/17/2025"
        public string JobID { get; set; } // "JP0003"
        public string JobPosition { get; set; } // "Deputy Director, SAQA"
        public string EmploymentType { get; set; } // "PERMANENT"
        public string DepartmentCode { get; set; } // "ADM"
        public string DirectorateName { get; set; } // "Administration"
        public string ReasonForRecruitment { get; set; } // "New Position"
        public int Positions { get; set; } // 1
        public string JobGrade { get; set; } // "KNQA 7"
        public decimal GrossSalary { get; set; } // 0.00
        public string ContractPeriod { get; set; }
        public bool HasGratuity { get; set; }
        public string ApplicationStartDate { get; set; }
        public string ApplicationDeadline { get; set; }
        public string ExpectedReportingDate { get; set; }
        public string RequestedBy { get; set; } // "MUINDI"
        public string Status { get; set; } // "Approved"
        public bool ShortListingRequired { get; set; }
        public int ShortlistingThreshold { get; set; } // 0
        public string EmployeeNo { get; set; } // "KNQA/0019/2021"
        public string RaisedBy { get; set; } // "Recho Chepkurui MUTHURI"
        public bool Posted { get; set; }
        public string ODataEtag { get; set; }

        // Additional fields for display/processing
        public string CreatedDate { get; set; }
        public string LastModifiedDate { get; set; }
        public string ApproverName { get; set; }
        public string ApproverComments { get; set; }
    }

    public class JobRequisitionCreate
    {
        public string JobPosition { get; set; }
        public string EmploymentType { get; set; }
        public string DepartmentCode { get; set; }
        public string ReasonForRecruitment { get; set; }
        public int Positions { get; set; }
        public string JobGrade { get; set; }
        public decimal GrossSalary { get; set; }
        public string ContractPeriod { get; set; }
        public bool HasGratuity { get; set; }
        public string ApplicationStartDate { get; set; }
        public string ApplicationDeadline { get; set; }
        public string ExpectedReportingDate { get; set; }
        public string RequestedBy { get; set; }
        public bool ShortListingRequired { get; set; }
        public int ShortlistingThreshold { get; set; }
    }

    public class JobRequisitionUpdate
    {
        public string ApplicationNo { get; set; }
        public string JobPosition { get; set; }
        public string EmploymentType { get; set; }
        public string DepartmentCode { get; set; }
        public string ReasonForRecruitment { get; set; }
        public int Positions { get; set; }
        public string JobGrade { get; set; }
        public decimal GrossSalary { get; set; }
        public string ContractPeriod { get; set; }
        public bool HasGratuity { get; set; }
        public string ApplicationStartDate { get; set; }
        public string ApplicationDeadline { get; set; }
        public string ExpectedReportingDate { get; set; }
        public string RequestedBy { get; set; }
        public string Status { get; set; }
        public bool ShortListingRequired { get; set; }
        public int ShortlistingThreshold { get; set; }
        public bool Posted { get; set; }
    }

    public class JobRequisitionFilter
    {
        public string DepartmentCode { get; set; }
        public string Status { get; set; }
        public string EmploymentType { get; set; }
        public string SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string OrderBy { get; set; } = "Document_Date desc";
    }

    public class JobRequisitionSummary
    {
        public int TotalRequisitions { get; set; }
        public int PendingRequisitions { get; set; }
        public int ApprovedRequisitions { get; set; }
        public int RejectedRequisitions { get; set; }
        public int OpenPositions { get; set; }
        public int FilledPositions { get; set; }
        public decimal TotalBudget { get; set; }
    }

    public class JobRequisitionODataResponse
    {
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; }

        [JsonProperty("value")]
        public List<JobRequisition> Value { get; set; }
    }
}
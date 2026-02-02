using Newtonsoft.Json;

namespace KNQASelfService.Models
{
    /// <summary>
    /// Represents a Leave Plan from Business Central (Listing view)
    /// Models the Leave_Plan_List table structure
    /// </summary>
    public class LeavePlan
    {
        [JsonProperty("@odata.etag")]
        public string ODataEtag { get; set; } = "";

        [JsonProperty("Application_No")]
        public string ApplicationNo { get; set; } = "";

        [JsonProperty("Maturity_Date")]
        public string MaturityDate { get; set; } = "";

        [JsonProperty("Employee_No")]
        public string EmployeeNo { get; set; } = "";

        [JsonProperty("Employee_Name")]
        public string EmployeeName { get; set; } = "";

        [JsonProperty("Designation")]
        public string Designation { get; set; } = "";

        [JsonProperty("Department_Code")]
        public string DepartmentCode { get; set; } = "";

        [JsonProperty("Date_Of_Joining_Company")]
        public string DateOfJoiningCompany { get; set; } = "";

        [JsonProperty("Leave_Code")]
        public string LeaveCode { get; set; } = "";

        [JsonProperty("Fiscal_Start_Date")]
        public string FiscalStartDate { get; set; } = "";

        [JsonProperty("Leave_Entitlement")]
        public int LeaveEntitlement { get; set; }

        [JsonProperty("Leave_Earned_to_Date")]
        public int LeaveEarnedToDate { get; set; }

        [JsonProperty("Leave_Balance")]
        public int LeaveBalance { get; set; }

        [JsonProperty("Days_in_Plan")]
        public int DaysInPlan { get; set; }

        [JsonProperty("User_ID")]
        public string UserId { get; set; } = "";

        [JsonProperty("Application_Date")]
        public string ApplicationDate { get; set; } = "";

        [JsonProperty("Status")]
        public string Status { get; set; } = "";

        [JsonProperty("No_series")]
        public string NoSeries { get; set; } = "";

        [JsonProperty("Off_Days")]
        public int OffDays { get; set; }
    }

    /// <summary>
    /// Request model for creating/updating a Leave Plan Application
    /// This goes to Leave_Plan_Application API which may have different structure
    /// </summary>
    public class LeavePlanCreate
    {
        public string EmployeeNo { get; set; } = "";
        public string LeaveCode { get; set; } = "";
        public int DaysInPlan { get; set; }
        public string FiscalStartDate { get; set; } = "";
        public int LeaveEntitlement { get; set; }
        public string MaturityDate { get; set; } = "";
    }

    /// <summary>
    /// Request model for updating a Leave Plan Application
    /// </summary>
    public class LeavePlanUpdate
    {
        public string ApplicationNo { get; set; } = "";
        public string LeaveCode { get; set; } = "";
        public int DaysInPlan { get; set; }
        public int LeaveEntitlement { get; set; }
        public string FiscalStartDate { get; set; } = "";
        public string MaturityDate { get; set; } = "";
    }

    /// <summary>
    /// Model for Leave Plan Application (different from Leave Plan List)
    /// This matches the Leave_Plan_Application API structure
    /// </summary>
    public class LeavePlanApplication
    {
        [JsonProperty("@odata.etag")]
        public string ODataEtag { get; set; } = "";

        [JsonProperty("Application_No")]
        public string ApplicationNo { get; set; } = "";

        [JsonProperty("Maturity_Date")]
        public string MaturityDate { get; set; } = "";

        [JsonProperty("Employee_No")]
        public string EmployeeNo { get; set; } = "";

        [JsonProperty("Employee_Name")]
        public string EmployeeName { get; set; } = "";

        [JsonProperty("Designation")]
        public string Designation { get; set; } = "";

        [JsonProperty("Department_Code")]
        public string DepartmentCode { get; set; } = "";

        [JsonProperty("Date_Of_Joining_Company")]
        public string DateOfJoiningCompany { get; set; } = "";

        [JsonProperty("Leave_Code")]
        public string LeaveCode { get; set; } = "";

        [JsonProperty("Fiscal_Start_Date")]
        public string FiscalStartDate { get; set; } = "";

        [JsonProperty("Leave_Entitlement")]
        public int LeaveEntitlement { get; set; }

        [JsonProperty("Leave_Earned_to_Date")]
        public int LeaveEarnedToDate { get; set; }

        [JsonProperty("Leave_Balance")]
        public int LeaveBalance { get; set; }

        [JsonProperty("Days_in_Plan")]
        public int DaysInPlan { get; set; }

        [JsonProperty("User_ID")]
        public string UserId { get; set; } = "";

        [JsonProperty("Application_Date")]
        public string ApplicationDate { get; set; } = "";

        [JsonProperty("Off_Days")]
        public int OffDays { get; set; }
    }

    /// <summary>
    /// OData collection response wrapper for Leave_Plan_List
    /// </summary>
    public class LeavePlanODataResponse
    {
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; } = "";

        [JsonProperty("value")]
        public List<LeavePlan> Value { get; set; } = new();
    }

    /// <summary>
    /// OData collection response wrapper for Leave_Plan_Application
    /// </summary>
    public class LeavePlanApplicationODataResponse
    {
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; } = "";

        [JsonProperty("value")]
        public List<LeavePlanApplication> Value { get; set; } = new();
    }

    /// <summary>
    /// Model for filtering Leave Plans
    /// </summary>
    public class LeavePlanFilter
    {
        public string EmployeeNo { get; set; } = "";
        public string Status { get; set; } = "";
        public string LeaveCode { get; set; } = "";
        public string SearchTerm { get; set; } = "";
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public string OrderBy { get; set; } = "Application_Date desc";
    }

    /// <summary>
    /// Summary statistics for Leave Plans
    /// </summary>
    public class LeavePlanSummary
    {
        public int TotalPlans { get; set; }
        public int ActivePlans { get; set; }
        public int InactiveePlans { get; set; }
        public int TotalEntitlement { get; set; }
        public int TotalBalance { get; set; }
    }
}
using Newtonsoft.Json;

namespace KNQASelfService.Models
{
    public class LeaveApplication
    {
        [JsonProperty("@odata.etag")]
        public string ODataEtag { get; set; }

        [JsonProperty("Application_No")]
        public string ApplicationNo { get; set; }

        [JsonProperty("Employee_No")]
        public string EmployeeNo { get; set; }

        [JsonProperty("Employee_Name")]
        public string EmployeeName { get; set; }

        [JsonProperty("Leave_Code")]
        public string LeaveCode { get; set; }

        [JsonProperty("Days_Applied")]
        public int DaysApplied { get; set; }

        [JsonProperty("Start_Date")]
        public string StartDate { get; set; }

        [JsonProperty("End_Date")]
        public string EndDate { get; set; }

        [JsonProperty("Application_Date")]
        public string ApplicationDate { get; set; }

        [JsonProperty("Resumption_Date")]
        public string ResumptionDate { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("Balance_brought_forward")]
        public int BalanceBroughtForward { get; set; }

        [JsonProperty("Leave_Entitlment")]
        public int LeaveEntitlement { get; set; }

        [JsonProperty("Total_Leave_Days_Taken")]
        public int TotalLeaveDaysTaken { get; set; }

        [JsonProperty("Leave_balance")]
        public int LeaveBalance { get; set; }

        [JsonProperty("Duties_Taken_Over_By")]
        public string DutiesTakenOverBy { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Mobile_No")]
        public string MobileNo { get; set; }

        [JsonProperty("Leave_Earned_to_Date")]
        public int LeaveEarnedToDate { get; set; }

        [JsonProperty("Maturity_Date")]
        public string MaturityDate { get; set; }

        [JsonProperty("Date_of_Joining_Company")]
        public string DateOfJoiningCompany { get; set; }

        [JsonProperty("Department_Code")]
        public string DepartmentCode { get; set; }

        [JsonProperty("Department_Name")]
        public string DepartmentName { get; set; }

        [JsonProperty("User_ID")]
        public string UserId { get; set; }

        [JsonProperty("Pending_Approver")]
        public string PendingApprover { get; set; }

        // ADD THESE MISSING PROPERTIES
        [JsonProperty("Reason")]
        public string Reason { get; set; }

        [JsonProperty("Telephone_No")]
        public string TelephoneNo { get; set; }

        [JsonProperty("Alternate_Phone_No")]
        public string AlternatePhoneNo { get; set; }
    }

    public class LeaveApplicationHR
    {
        [JsonProperty("@odata.etag")]
        public string ODataEtag { get; set; }

        [JsonProperty("Application_No")]
        public string ApplicationNo { get; set; }

        [JsonProperty("Employee_No")]
        public string EmployeeNo { get; set; }

        [JsonProperty("Employee_Name")]
        public string EmployeeName { get; set; }

        [JsonProperty("Leave_Code")]
        public string LeaveCode { get; set; }

        [JsonProperty("Days_Applied")]
        public int DaysApplied { get; set; }

        [JsonProperty("Start_Date")]
        public string StartDate { get; set; }

        [JsonProperty("End_Date")]
        public string EndDate { get; set; }

        [JsonProperty("Application_Date")]
        public string ApplicationDate { get; set; }

        [JsonProperty("Resumption_Date")]
        public string ResumptionDate { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("Balance_brought_forward")]
        public int BalanceBroughtForward { get; set; }

        [JsonProperty("Leave_Entitlment")]
        public int LeaveEntitlement { get; set; }

        [JsonProperty("Total_Leave_Days_Taken")]
        public int TotalLeaveDaysTaken { get; set; }

        [JsonProperty("Leave_balance")]
        public int LeaveBalance { get; set; }

        [JsonProperty("Duties_Taken_Over_By")]
        public string DutiesTakenOverBy { get; set; }

        [JsonProperty("Name")]
        public string HandoverPersonName { get; set; }

        [JsonProperty("Mobile_No")]
        public string MobileNo { get; set; }

        [JsonProperty("Alternate_Phone_No")]
        public string AlternatePhoneNo { get; set; }

        [JsonProperty("Leave_Earned_to_Date")]
        public int LeaveEarnedToDate { get; set; }

        [JsonProperty("Maturity_Date")]
        public string MaturityDate { get; set; }

        [JsonProperty("Date_of_Joining_Company")]
        public string DateOfJoiningCompany { get; set; }

        [JsonProperty("Department_Code")]
        public string DepartmentCode { get; set; }

        [JsonProperty("Department_Name")]
        public string DepartmentName { get; set; }

        [JsonProperty("User_ID")]
        public string UserId { get; set; }

        [JsonProperty("Pending_Approver")]
        public string PendingApprover { get; set; }

        [JsonProperty("Reason")]
        public string Reason { get; set; }

        [JsonProperty("Telephone_No")]
        public string TelephoneNo { get; set; }

        // Add these properties if they exist in your BC table
        [JsonProperty("Incude_Leave_Allowance")]
        public bool IncludeLeaveAllowance { get; set; }

        [JsonProperty("Leave_Allowance")]
        public decimal LeaveAllowance { get; set; }

        [JsonProperty("Annual_Leave_Entitlement_Bal")]
        public int AnnualLeaveEntitlementBal { get; set; }

        [JsonProperty("Recalled_Days")]
        public int RecalledDays { get; set; }

        [JsonProperty("Days_Absent")]
        public int DaysAbsent { get; set; }

        [JsonProperty("No_of_Approvals")]
        public int NoOfApprovals { get; set; }
    }

    public class LeaveApplicationCreate
    {
        [JsonProperty("Employee_No")]
        public string EmployeeNo { get; set; }

        [JsonProperty("Leave_Code")]
        public string LeaveCode { get; set; }

        [JsonProperty("Days_Applied")]
        public int DaysApplied { get; set; }

        [JsonProperty("Start_Date")]
        public string StartDate { get; set; }

        [JsonProperty("Duties_Taken_Over_By")]
        public string DutiesTakenOverBy { get; set; }

        [JsonProperty("Reason")]
        public string Reason { get; set; }

        [JsonProperty("Telephone_No")]
        public string TelephoneNo { get; set; }

        [JsonProperty("Alternate_Phone_No")]
        public string AlternatePhoneNo { get; set; }
    }

    public class UserSetup
    {
        [JsonProperty("User_ID")]
        public string UserId { get; set; }

        [JsonProperty("Employee_No")]
        public string EmployeeNo { get; set; }

        [JsonProperty("Department")]
        public string Department { get; set; }
    }

    public class LeaveEntitlement
    {
        [JsonProperty("@odata.etag")]
        public string ODataEtag { get; set; }

        [JsonProperty("Employee_No")]
        public string EmployeeNo { get; set; }

        [JsonProperty("Leave_Code")]
        public string LeaveCode { get; set; }

        [JsonProperty("Total_Days")]
        public decimal? TotalDays { get; set; }

        [JsonProperty("Days_Taken")]
        public decimal? DaysTaken { get; set; }

        [JsonProperty("Remaining_Days")]
        public decimal? RemainingDays { get; set; }

        [JsonProperty("Balance_Brought_Forward")]
        public decimal? BalanceBroughtForward { get; set; }

        [JsonProperty("Leave_Earned_To_Date")]
        public decimal? LeaveEarnedToDate { get; set; }

        [JsonProperty("Leave_Year")]
        public int? LeaveYear { get; set; }
    }

    public class LeaveType
    {
        [JsonProperty("@odata.etag")]
        public string ODataEtag { get; set; }

        [JsonProperty("Code")]
        public string Code { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("Days")]
        public int Days { get; set; }

        [JsonProperty("Accrue_Days")]
        public bool AccrueDays { get; set; }

        [JsonProperty("Accrual_Period")]
        public string AccrualPeriod { get; set; }

        [JsonProperty("Days_to_Accrue")]
        public int DaysToAccrue { get; set; }

        [JsonProperty("Contract_Days")]
        public int ContractDays { get; set; }

        [JsonProperty("Unlimited_Days")]
        public bool UnlimitedDays { get; set; }

        [JsonProperty("Gender")]
        public string Gender { get; set; }

        [JsonProperty("Balance")]
        public string Balance { get; set; }

        [JsonProperty("Max_Carry_Forward_Days")]
        public int MaxCarryForwardDays { get; set; }

        [JsonProperty("Annual_Leave")]
        public bool AnnualLeave { get; set; }

        [JsonProperty("Inclusive_of_Holidays")]
        public bool Inclusive_of_Holidays { get; set; }

        [JsonProperty("Inclusive_of_Saturday")]
        public bool Inclusive_of_Saturday { get; set; }

        [JsonProperty("Inclusive_of_Sunday")]
        public bool Inclusive_of_Sunday { get; set; }

        [JsonProperty("Off_Holidays_Days_Leave")]
        public bool OffHolidaysDaysLeave { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("Eligible_Staff")]
        public string EligibleStaff { get; set; }
    }

        public class Employee
        {
            [JsonProperty("No")]
            public string No { get; set; } = string.Empty;

            [JsonProperty("FullName")]
            public string FullName { get; set; } = string.Empty;

            [JsonProperty("First_Name")]
            public string FirstName { get; set; } = string.Empty;

            [JsonProperty("Last_Name")]
            public string LastName { get; set; } = string.Empty;

            [JsonProperty("Status")]
            public string Status { get; set; } = string.Empty;

            [JsonProperty("Job_Title")]
            public string JobTitle { get; set; } = string.Empty;

            [JsonProperty("Global_Dimension_1_Code")]
            public string DepartmentCode { get; set; } = string.Empty;

            [JsonProperty("E_Mail")]
            public string Email { get; set; } = string.Empty;

            // Add this property for Employment Date
            [JsonProperty("Employment_Date")]
            public DateTime? EmploymentDate { get; set; }

            // Manager information
            [JsonProperty("Reports_To_No")]
            public string ManagerNo { get; set; } = string.Empty;

            // Helper property for display
            [JsonIgnore]
            public string DisplayName => $"{FullName} ({No})";
        }
    
}
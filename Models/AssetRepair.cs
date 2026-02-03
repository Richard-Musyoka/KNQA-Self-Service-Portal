using System;
using System.ComponentModel.DataAnnotations;

namespace KNQASelfService.Models
{
    public class AssetRepair
    {
        public string? MaintenanceRefNo { get; set; }

        [Required(ErrorMessage = "Employee No is required")]
        public string? EmployeeNo { get; set; }

        public string? EmployeeName { get; set; }
        public string? JobTitle { get; set; }
        public string? SupervisorName { get; set; }
        public string? Department { get; set; }

        [Required(ErrorMessage = "Maintenance Status is required")]
        public string? MaintenanceStatus { get; set; } = "Open";

        public string? MaintenanceDetails { get; set; }

        [Required(ErrorMessage = "Asset No is required")]
        public string? AssetNo { get; set; }

        public string? AssetName { get; set; }

        [Required(ErrorMessage = "Maintenance Issue is required")]
        public string? MaintenanceIssue { get; set; }

        public DateTime? ReportedDate { get; set; } = DateTime.Now;
        public DateTime? CompletedDate { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? ActualCost { get; set; }
        public string? RepairPriority { get; set; } = "Medium"; // Low, Medium, High, Critical
        public string? AssignedTechnician { get; set; }
        public string? ResolutionDetails { get; set; }
        public string? Remarks { get; set; }
        public string? ODataEtag { get; set; }
    }

    public class AssetRepairFilter
    {
        public string? SearchTerm { get; set; }
        public string? EmployeeNo { get; set; }
        public string? AssetNo { get; set; }
        public string? MaintenanceStatus { get; set; }
        public string? Department { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? RepairPriority { get; set; }
        public string? OrderBy { get; set; } = "ReportedDate desc";
    }

    public class AssetRepairSummary
    {
        public int TotalRepairs { get; set; }
        public int OpenRepairs { get; set; }
        public int InProgressRepairs { get; set; }
        public int CompletedRepairs { get; set; }
        public int CriticalRepairs { get; set; }
        public int MyOpenRepairs { get; set; }
        public decimal TotalEstimatedCost { get; set; }
        public decimal TotalActualCost { get; set; }
        public int AverageCompletionDays { get; set; }
    }

    public static class RepairStatus
    {
        public const string OPEN = "Open";
        public const string IN_PROGRESS = "In Progress";
        public const string COMPLETED = "Completed";
        public const string CANCELLED = "Cancelled";
        public const string ON_HOLD = "On Hold";
    }

    public static class RepairPriority
    {
        public const string LOW = "Low";
        public const string MEDIUM = "Medium";
        public const string HIGH = "High";
        public const string CRITICAL = "Critical";
    }
}
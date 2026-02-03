namespace KNQASelfService.Models
{
    /// <summary>
    /// Represents a Fixed Asset in the system
    /// </summary>
    public class FixedAsset
    {
        public string No { get; set; } = string.Empty;
        public string FAOldNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FixedAssetClassCode { get; set; } = string.Empty;
        public string TangibleIntangible { get; set; } = string.Empty;
        public string ClassCode { get; set; } = string.Empty;
        public string SubclassCode { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string TagNo { get; set; } = string.Empty;
        public string SerialNo { get; set; } = string.Empty;
        public string RegistrationNo { get; set; } = string.Empty;
        public bool Active { get; set; }
        public string AcqDate { get; set; } = string.Empty;
        public string SearchDescription { get; set; } = string.Empty;
        public string AllocationType { get; set; } = string.Empty;
        public string EmployeeCustomer { get; set; } = string.Empty;
        public string ResponsibleEmployee { get; set; } = string.Empty;
        public string ResponsibleEmployeeName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string KnqaCode { get; set; } = string.Empty;

        // Additional fields
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string WarrantyExpiryDate { get; set; } = string.Empty;
        public decimal AcquisitionCost { get; set; }
        public decimal BookValue { get; set; }
        public string LastMaintenanceDate { get; set; } = string.Empty;
        public string NextMaintenanceDate { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string ODataEtag { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model for creating/updating a Fixed Asset allocation
    /// </summary>
    public class FixedAssetAllocation
    {
        public string AssetNo { get; set; } = string.Empty;
        public string ResponsibleEmployee { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string AllocationDate { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    /// <summary>
    /// Summary statistics for Fixed Assets
    /// </summary>
    public class FixedAssetSummary
    {
        public int TotalAssets { get; set; }
        public int ActiveAssets { get; set; }
        public int InactiveAssets { get; set; }
        public int AllocatedAssets { get; set; }
        public int UnallocatedAssets { get; set; }
        public int TangibleAssets { get; set; }
        public int IntangibleAssets { get; set; }
        public decimal TotalAcquisitionCost { get; set; }
        public decimal TotalBookValue { get; set; }
    }

    /// <summary>
    /// Filter model for Fixed Assets
    /// </summary>
    public class FixedAssetFilter
    {
        public string? SearchTerm { get; set; }
        public string? FixedAssetClassCode { get; set; }
        public string? TangibleIntangible { get; set; }
        public string? Location { get; set; }
        public string? ResponsibleEmployee { get; set; }
        public string? DepartmentCode { get; set; }
        public bool? ActiveOnly { get; set; }
        public string? AllocationType { get; set; }
        public DateTime? AcqDateFrom { get; set; }
        public DateTime? AcqDateTo { get; set; }
        public string OrderBy { get; set; } = "No asc";
    }

    /// <summary>
    /// Asset maintenance record
    /// </summary>
    public class AssetMaintenance
    {
        public string AssetNo { get; set; } = string.Empty;
        public string MaintenanceDate { get; set; } = string.Empty;
        public string MaintenanceType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public string NextMaintenanceDate { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    /// <summary>
    /// Asset condition states
    /// </summary>
    public static class AssetCondition
    {
        public const string EXCELLENT = "Excellent";
        public const string GOOD = "Good";
        public const string FAIR = "Fair";
        public const string POOR = "Poor";
        public const string DAMAGED = "Damaged";
        public const string UNDER_REPAIR = "Under Repair";
        public const string OBSOLETE = "Obsolete";
    }

    /// <summary>
    /// Asset allocation types
    /// </summary>
    public static class AssetAllocationType
    {
        public const string EMPLOYEE = "Employee";
        public const string DEPARTMENT = "Department";
        public const string LOCATION = "Location";
        public const string PROJECT = "Project";
        public const string UNALLOCATED = "Unallocated";
    }

    /// <summary>
    /// Asset types
    /// </summary>
    public static class AssetType
    {
        public const string TANGIBLE = "Tangible";
        public const string INTANGIBLE = "Intangible";
    }

    /// <summary>
    /// Fixed asset classes
    /// </summary>
    public static class FixedAssetClass
    {
        public const string COMPUTER_EQUIPMENT = "Computer Equipment";
        public const string FURNITURE = "Furniture";
        public const string VEHICLES = "Vehicles";
        public const string MACHINERY = "Machinery";
        public const string BUILDINGS = "Buildings";
        public const string LAND = "Land";
        public const string SOFTWARE = "Software";
        public const string LICENSES = "Licenses";
        public const string OFFICE_EQUIPMENT = "Office Equipment";
        public const string OTHER = "Other";
    }
}
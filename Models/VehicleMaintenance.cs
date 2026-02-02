// Models/VehicleMaintenance.cs
using System;
using System.Text.Json.Serialization;

namespace KNQASelfService.Models
{
    public class VehicleMaintenance
    {
        [JsonPropertyName("No")]
        public string No { get; set; } = "";

        [JsonPropertyName("Document_Type")]
        public string DocumentType { get; set; } = "Maintenance";

        [JsonPropertyName("FA_Code_No")]
        public string FACodeNo { get; set; } = "";

        [JsonPropertyName("Vehicle_Registration_No")]
        public string VehicleRegistrationNo { get; set; } = "";

        [JsonPropertyName("Description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("Maintenance_Date")]
        public string MaintenanceDate { get; set; } = "";

        [JsonPropertyName("Status")]
        public string Status { get; set; } = "New";

        [JsonPropertyName("Maintenance_Type")]
        public string MaintenanceType { get; set; } = ""; // "New", "Repair/Maintenance"

        [JsonPropertyName("Pre_Service_Mileage")]
        public decimal PreServiceMileage { get; set; }

        [JsonPropertyName("Post_Service_Mileage")]
        public decimal PostServiceMileage { get; set; }

        [JsonPropertyName("Total_Repair_Cost")]
        public decimal TotalRepairCost { get; set; }

        [JsonPropertyName("Total_Maintenance_Cost")]
        public decimal TotalMaintenanceCost { get; set; }

        [JsonPropertyName("Total_Cost")]
        public decimal TotalCost { get; set; }

        [JsonPropertyName("Mileage_Difference")]
        public decimal MileageDifference { get; set; }

        [JsonPropertyName("Maintenance_Vendor_No")]
        public string MaintenanceVendorNo { get; set; } = "";

        [JsonPropertyName("Vendor_Name")]
        public string VendorName { get; set; } = "";

        [JsonPropertyName("Service_Description")]
        public string ServiceDescription { get; set; } = "";

        [JsonPropertyName("Next_Service_Date")]
        public string NextServiceDate { get; set; } = "";

        [JsonPropertyName("Next_Service_Mileage")]
        public decimal NextServiceMileage { get; set; }

        [JsonPropertyName("Service_Due_Date")]
        public string ServiceDueDate { get; set; } = "";

        [JsonPropertyName("Remarks")]
        public string Remarks { get; set; } = "";

        [JsonPropertyName("Created_By")]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("Created_Date")]
        public string CreatedDate { get; set; } = "";

        [JsonPropertyName("Created_Time")]
        public string CreatedTime { get; set; } = "";

        // OData metadata
        [JsonPropertyName("@odata.etag")]
        public string ODataEtag { get; set; } = "";

        // Navigation properties
        public FleetVehicle VehicleDetails { get; set; }
    }

    // Create DTO for new maintenance records
    public class VehicleMaintenanceCreate
    {
        public string FACodeNo { get; set; } = "";
        public string VehicleRegistrationNo { get; set; } = "";
        public string MaintenanceDate { get; set; } = "";
        public string MaintenanceType { get; set; } = "";
        public decimal PreServiceMileage { get; set; }
        public decimal TotalRepairCost { get; set; }
        public decimal TotalMaintenanceCost { get; set; }
        public string MaintenanceVendorNo { get; set; } = "";
        public string ServiceDescription { get; set; } = "";
        public string NextServiceDate { get; set; } = "";
        public decimal NextServiceMileage { get; set; }
        public string Remarks { get; set; } = "";
        public string CreatedBy { get; set; } = "";
    }

    // Summary statistics
    public class MaintenanceSummary
    {
        public int TotalMaintenance { get; set; }
        public int CompletedMaintenance { get; set; }
        public int PendingMaintenance { get; set; }
        public int OverdueMaintenance { get; set; }
        public decimal TotalCostThisMonth { get; set; }
        public decimal TotalCostThisYear { get; set; }
        public List<MaintenanceByType> MaintenanceByType { get; set; } = new();
        public List<MaintenanceByVehicle> MaintenanceByVehicle { get; set; } = new();
        public List<UpcomingMaintenance> UpcomingMaintenance { get; set; } = new();
    }

    public class MaintenanceByType
    {
        public string MaintenanceType { get; set; } = "";
        public int Count { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class MaintenanceByVehicle
    {
        public string VehicleNo { get; set; } = "";
        public string VehicleDescription { get; set; } = "";
        public string RegistrationNo { get; set; } = "";
        public int MaintenanceCount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageCost { get; set; }
    }

    public class UpcomingMaintenance
    {
        public string VehicleNo { get; set; } = "";
        public string VehicleDescription { get; set; } = "";
        public string RegistrationNo { get; set; } = "";
        public string NextServiceDate { get; set; } = "";
        public decimal NextServiceMileage { get; set; }
        public decimal CurrentMileage { get; set; }
        public string Status { get; set; } = "";
    }

    // Maintenance types
    public static class MaintenanceType
    {
        public const string NEW = "New";
        public const string REPAIR = "Repair/Maintenance";
        public const string SCHEDULED = "Scheduled Service";
        public const string EMERGENCY = "Emergency Repair";
        public const string PREVENTIVE = "Preventive Maintenance";
        public const string CORRECTIVE = "Corrective Maintenance";
        public const string BREAKDOWN = "Breakdown Maintenance";
    }

    // Maintenance status
    public static class MaintenanceStatus
    {
        public const string NEW = "New";
        public const string IN_PROGRESS = "In Progress";
        public const string COMPLETED = "Completed";
        public const string CANCELLED = "Cancelled";
        public const string PENDING_APPROVAL = "Pending Approval";
        public const string APPROVED = "Approved";
        public const string REJECTED = "Rejected";
    }
}
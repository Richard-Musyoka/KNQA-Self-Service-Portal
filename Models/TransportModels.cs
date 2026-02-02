using System.Text.Json.Serialization;

namespace KNQASelfService.Models
{
    public class FleetVehicle
    {
        [JsonPropertyName("No")]
        public string No { get; set; } = "";

        [JsonPropertyName("Description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("Chassis_No")]
        public string ChassisNo { get; set; } = "";

        [JsonPropertyName("Registration_No")]
        public string RegistrationNo { get; set; } = "";

        [JsonPropertyName("Engine_No")]
        public string EngineNo { get; set; } = "";

        [JsonPropertyName("Responsible_Employee")]
        public string ResponsibleEmployee { get; set; } = "";

        [JsonPropertyName("Colour")]
        public string Colour { get; set; } = "";

        [JsonPropertyName("Vendor_No")]
        public string VendorNo { get; set; } = "";

        [JsonPropertyName("Maintenance_Vendor_No")]
        public string MaintenanceVendorNo { get; set; } = "";

        [JsonPropertyName("Status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("Vehicle_Type")]
        public string VehicleType { get; set; } = "";

        [JsonPropertyName("Capacity")]
        public decimal Capacity { get; set; }

        [JsonPropertyName("Fuel_Type")]
        public string FuelType { get; set; } = "";

        [JsonPropertyName("Year_of_Make")]
        public int YearOfMake { get; set; }

        [JsonPropertyName("Current_Location")]
        public string CurrentLocation { get; set; } = "";

        // Helper property for display
        public string DisplayInfo => $"{Description} ({RegistrationNo}) - {VehicleType}";
    }

    public class TransportRequest
    {
        [JsonPropertyName("Request_No")]
        public string RequestNo { get; set; } = "";

        [JsonPropertyName("Request_Date")]
        public string RequestDate { get; set; } = "";

        [JsonPropertyName("Employee_No")]
        public string EmployeeNo { get; set; } = "";

        [JsonPropertyName("Employee_Name")]
        public string EmployeeName { get; set; } = "";

        [JsonPropertyName("Purpose_of_Travel")]
        public string Purpose { get; set; } = "";

        [JsonPropertyName("Destination_Itinerary")]
        public string Destination { get; set; } = "";

        [JsonPropertyName("Trip_Planned_Start_Date")]
        public string StartDate { get; set; } = "";

        [JsonPropertyName("Trip_Planned_End_Date")]
        public string EndDate { get; set; } = "";

        [JsonPropertyName("Start_Time")]
        public string StartTime { get; set; } = "";

        [JsonPropertyName("Return_Time")]
        public string ReturnTime { get; set; } = "";

        [JsonPropertyName("No_of_Employees_Travelling")]
        public decimal NoOfEmployees { get; set; }

        [JsonPropertyName("No_of_Confirmed_Employee")]
        public decimal NoOfConfirmedEmployees { get; set; }

        [JsonPropertyName("No_of_Non_Employees")]
        public decimal NoOfNonEmployees { get; set; }

        [JsonPropertyName("Status")]
        public string Status { get; set; } = "Pending Approval";

        [JsonPropertyName("No_of_Approvals")]
        public decimal NoOfApprovals { get; set; }

        [JsonPropertyName("Vehicle_Allocated")]
        public string VehicleAllocated { get; set; } = "";

        [JsonPropertyName("Vehicle_Description")]
        public string VehicleDescription { get; set; } = "";

        [JsonPropertyName("Driver")]
        public string DriverNo { get; set; } = "";

        [JsonPropertyName("Driver_Name")]
        public string DriverName { get; set; } = "";

        [JsonPropertyName("Odometer_Reading_Before")]
        public decimal OdometerBefore { get; set; }

        [JsonPropertyName("Odometer_Reading_After")]
        public decimal OdometerAfter { get; set; }

        [JsonPropertyName("Number_of_Passengers")]
        public decimal NumberOfPassengers { get; set; }

        [JsonPropertyName("@odata.etag")]
        public string ODataEtag { get; set; } = "";
    }

    public class TransportRequestCreate
    {
        public string EmployeeNo { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Purpose { get; set; } = "";
        public string Destination { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string ReturnTime { get; set; } = "";
        public decimal NoOfEmployees { get; set; } = 1;
        public decimal NoOfNonEmployees { get; set; } = 0;
        public string VehicleAllocated { get; set; } = "";
        public string VehicleDescription { get; set; } = "";
        public string DriverNo { get; set; } = "";
        public string DriverName { get; set; } = "";
        public decimal NumberOfPassengers { get; set; } = 1;
    }

    public class TravellingEmployee
    {
        [JsonPropertyName("Employee_No")]
        public string EmployeeNo { get; set; } = "";

        [JsonPropertyName("Employee_Name")]
        public string EmployeeName { get; set; } = "";

        [JsonPropertyName("Employee_Scale")]
        public string EmployeeScale { get; set; } = "";

        [JsonPropertyName("Start_Date")]
        public string StartDate { get; set; } = "";

        [JsonPropertyName("End_Date")]
        public string EndDate { get; set; } = "";

        [JsonPropertyName("No_of_Days")]
        public decimal NoOfDays { get; set; }

        [JsonPropertyName("Request_No")]
        public string RequestNo { get; set; } = "";

        [JsonPropertyName("@odata.etag")]
        public string ODataEtag { get; set; } = "";
    }

    public class TravellingEmployeeCreate
    {
        public string RequestNo { get; set; } = "";
        public string EmployeeNo { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string EmployeeScale { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public decimal NoOfDays { get; set; }
    }

    public class TransportRequestSummary
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int InProgressRequests { get; set; }
    }

    public static class TransportStatus
    {
        public const string PENDING = "Pending Approval";
        public const string APPROVED = "Approved";
        public const string REJECTED = "Rejected";
        public const string COMPLETED = "Completed";
        public const string IN_PROGRESS = "In Progress";
        public const string CANCELLED = "Cancelled";
        public const string VEHICLE_ALLOCATED = "Vehicle Allocated";
        public const string DRIVER_ASSIGNED = "Driver Assigned";
    }

    public static class VehicleStatus
    {
        public const string AVAILABLE = "Available";
        public const string BOOKED = "Booked";
        public const string IN_USE = "In Use";
        public const string MAINTENANCE = "Under Maintenance";
        public const string UNAVAILABLE = "Unavailable";
    }
}
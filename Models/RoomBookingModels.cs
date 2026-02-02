using System.Text.Json.Serialization;

namespace KNQASelfService.Models
{
    public class AvailableMeetingRoom
    {
        [JsonPropertyName("Room_No")]
        public string RoomNo { get; set; } = "";

        [JsonPropertyName("Room_Name")]
        public string RoomName { get; set; } = "";

        [JsonPropertyName("Room_Status")]
        public string RoomStatus { get; set; } = "";

        [JsonPropertyName("Room_Capacity")]
        public decimal RoomCapacity { get; set; }

        [JsonPropertyName("Location")]
        public string Location { get; set; } = "";
    }

    public class RoomBooking
    {
        [JsonPropertyName("Booking_No")]
        public string BookingNo { get; set; } = "";

        [JsonPropertyName("Room_No")]
        public string RoomNo { get; set; } = "";

        [JsonPropertyName("Room_Name")]
        public string RoomName { get; set; } = "";

        [JsonPropertyName("Room_Capacity")]
        public decimal RoomCapacity { get; set; }

        [JsonPropertyName("No_of_Participants")]
        public decimal NoOfParticipants { get; set; }

        [JsonPropertyName("Booking_Date")]
        public string Date { get; set; } = "";

        [JsonPropertyName("Start_Time")]
        public string StartTime { get; set; } = "";

        [JsonPropertyName("End_Time")]
        public string EndTime { get; set; } = "";

        [JsonPropertyName("Duration")]
        public string Duration { get; set; } = "";

        [JsonPropertyName("Status")]
        public string Status { get; set; } = "Pending Approval";

        [JsonPropertyName("Remarks")]
        public string Remarks { get; set; } = "";

        [JsonPropertyName("Special_Request")]
        public string SpecialRequest { get; set; } = "";

        [JsonPropertyName("Employee_No")]
        public string EmployeeNo { get; set; } = "";

        [JsonPropertyName("Employee_Name")]
        public string EmployeeName { get; set; } = "";

        [JsonPropertyName("Purpose")]
        public string Purpose { get; set; } = "";

        [JsonPropertyName("@odata.etag")]
        public string ODataEtag { get; set; } = "";
    }

    public class RoomBookingCreate
    {
        public string RoomNo { get; set; } = "";
        public string RoomName { get; set; } = "";
        public decimal RoomCapacity { get; set; }
        public decimal NoOfParticipants { get; set; }
        public string Date { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string Remarks { get; set; } = "";
        public string SpecialRequest { get; set; } = "";
        public string EmployeeNo { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Purpose { get; set; } = "";
        public string Duration { get; set; } = "";
    }

    public class RoomBookingSummary
    {
        public int TotalBookings { get; set; }
        public int ApprovedBookings { get; set; }
        public int PendingBookings { get; set; }
        public int RejectedBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
    }

    public static class BookingStatus
    {
        public const string PENDING = "Pending Approval";
        public const string APPROVED = "Approved";
        public const string REJECTED = "Rejected";
        public const string CANCELLED = "Cancelled";
        public const string COMPLETED = "Completed";
        public const string IN_PROGRESS = "In Progress";
    }

    public static class RoomStatus
    {
        public const string AVAILABLE = "Available";
        public const string BOOKED = "Booked";
        public const string OCCUPIED = "Occupied";
        public const string MAINTENANCE = "Under Maintenance";
        public const string UNAVAILABLE = "Unavailable";
    }
}
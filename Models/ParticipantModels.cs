using System.Text.Json.Serialization;

namespace KNQASelfService.Models
{
    public class MeetingParticipant
    {
        [JsonPropertyName("Participant_No")]
        public string ParticipantNo { get; set; } = "";

        [JsonPropertyName("Booking_No")]
        public string BookingNo { get; set; } = "";

        [JsonPropertyName("Employee_No")]
        public string EmployeeNo { get; set; } = "";

        [JsonPropertyName("Employee_Name")]
        public string EmployeeName { get; set; } = "";

        [JsonPropertyName("Job_Title")]
        public string JobTitle { get; set; } = "";

        [JsonPropertyName("Department")]
        public string Department { get; set; } = "";

        [JsonPropertyName("Email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("Is_External")]
        public bool IsExternal { get; set; }

        [JsonPropertyName("External_Name")]
        public string ExternalName { get; set; } = "";

        [JsonPropertyName("External_Email")]
        public string ExternalEmail { get; set; } = "";

        [JsonPropertyName("External_Company")]
        public string ExternalCompany { get; set; } = "";

        [JsonPropertyName("Status")]
        public string Status { get; set; } = "Invited";

        [JsonPropertyName("Response_Status")]
        public string ResponseStatus { get; set; } = "Pending";

        [JsonPropertyName("Response_Date")]
        public string ResponseDate { get; set; } = "";

        [JsonPropertyName("Response_Remarks")]
        public string ResponseRemarks { get; set; } = "";

        [JsonPropertyName("@odata.etag")]
        public string ODataEtag { get; set; } = "";
    }

    public class MeetingParticipantCreate
    {
        public string BookingNo { get; set; } = "";
        public string EmployeeNo { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public bool IsExternal { get; set; }
        public string ExternalName { get; set; } = "";
        public string ExternalEmail { get; set; } = "";
        public string ExternalCompany { get; set; } = "";
    }

    public static class ParticipantStatus
    {
        public const string INVITED = "Invited";
        public const string ATTENDING = "Attending";
        public const string DECLINED = "Declined";
        public const string CANCELLED = "Cancelled";
        public const string ATTENDED = "Attended";
    }

    public static class ResponseStatus
    {
        public const string PENDING = "Pending";
        public const string ACCEPTED = "Accepted";
        public const string DECLINED = "Declined";
        public const string TENTATIVE = "Tentative";
    }
}
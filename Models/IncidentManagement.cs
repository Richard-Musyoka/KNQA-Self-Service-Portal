using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;

namespace KNQASelfService.Models
{
    public class IncidentManagement
    {
        [JsonProperty("IncidentReference")]
        public string IncidentReference { get; set; } // "IC0001"

        [JsonProperty("EmployeeNo")]
        public string EmployeeNo { get; set; } // "KNQA/0019/2021"

        [JsonProperty("EmployeeName")]
        public string EmployeeName { get; set; } // "Recho Chepkurui MUTHURI"

        [JsonProperty("JobTitle")]
        public string JobTitle { get; set; } // "JP0005"

        [JsonProperty("Department")]
        public string Department { get; set; } // "CEO"

        [JsonProperty("IncidentStatus")]
        public string IncidentStatus { get; set; } // "Under-Review"

        [JsonProperty("IncidentDescription")]
        public string IncidentDescription { get; set; } // "there was a sec breach at the office"

        [JsonProperty("IncidentDate")]
        public string IncidentDate { get; set; } // "11/19/2025"

        [JsonProperty("IncidentTime")]
        public string IncidentTime { get; set; } // "1:00:00 PM"

        [JsonProperty("IncidenceLocation_Name")]
        public string IncidenceLocationName { get; set; } // "RECEPTION"

        [JsonProperty("IncidentType")]
        public string IncidentType { get; set; } // "Security Incident/Breach"

        [JsonProperty("@odata.etag")]
        public string ODataEtag { get; set; }
    }

    public class IncidentManagementCreate
    {
        public string EmployeeNo { get; set; }
        public string EmployeeName { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string IncidentDescription { get; set; }
        public string IncidentDate { get; set; }
        public string IncidentTime { get; set; }
        public string IncidenceLocationName { get; set; }
        public string IncidentType { get; set; }
    }

    public class IncidentManagementUpdate
    {
        public string IncidentReference { get; set; }
        public string IncidentDescription { get; set; }
        public string IncidenceLocationName { get; set; }
        public string IncidentType { get; set; }
    }

    public class IncidentManagementFilter
    {
        public string EmployeeNo { get; set; }
        public string Department { get; set; }
        public string IncidentStatus { get; set; }
        public string IncidentType { get; set; }
        public string SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string OrderBy { get; set; } = "Incident_Date desc";
    }

    public class IncidentManagementSummary
    {
        public int TotalIncidents { get; set; }
        public int OpenIncidents { get; set; }
        public int InProgressIncidents { get; set; }
        public int ResolvedIncidents { get; set; }
        public int ClosedIncidents { get; set; }
    }

    public class IncidentManagementODataResponse
    {
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; }

        [JsonProperty("value")]
        public List<IncidentManagement> Value { get; set; }
    }
}
namespace KNQASelfService.Models
{
    /// <summary>
    /// Represents a Help Desk Ticket in the system
    /// </summary>
    public class HelpDeskTicket
    {
        public string TicketNo { get; set; } = string.Empty;
        public string EmployeeNo { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string ShortcutDimension3Code { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string CreatedTime { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public string AssignedToName { get; set; } = string.Empty;
        public string ResolutionDate { get; set; } = string.Empty;
        public string ResolutionTime { get; set; } = string.Empty;
        public string ResolutionNotes { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Attachments { get; set; } = string.Empty;
        public string LastModifiedDate { get; set; } = string.Empty;
        public string LastModifiedTime { get; set; } = string.Empty;
        public string ODataEtag { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model for creating a new Help Desk Ticket
    /// </summary>
    public class HelpDeskTicketCreate
    {
        public string EmployeeNo { get; set; } = string.Empty;
        public string ShortcutDimension3Code { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
    }

    /// <summary>
    /// Summary statistics for Help Desk Tickets
    /// </summary>
    public class HelpDeskTicketSummary
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int HighPriorityTickets { get; set; }
        public int MediumPriorityTickets { get; set; }
        public int LowPriorityTickets { get; set; }
    }

    /// <summary>
    /// Ticket categories
    /// </summary>
    public static class TicketCategory
    {
        public const string IT_SUPPORT = "IT Support";
        public const string HR_INQUIRY = "HR Inquiry";
        public const string FINANCE = "Finance";
        public const string FACILITIES = "Facilities";
        public const string GENERAL = "General";
        public const string PAYROLL = "Payroll";
        public const string BENEFITS = "Benefits";
        public const string EQUIPMENT = "Equipment";
        public const string ACCESS = "Access";
        public const string OTHER = "Other";
    }

    /// <summary>
    /// Ticket priorities
    /// </summary>
    public static class TicketPriority
    {
        public const string LOW = "Low";
        public const string MEDIUM = "Medium";
        public const string HIGH = "High";
        public const string URGENT = "Urgent";
    }

    /// <summary>
    /// Ticket statuses
    /// </summary>
    public static class TicketStatus
    {
        public const string OPEN = "Open";
        public const string IN_PROGRESS = "In Progress";
        public const string PENDING = "Pending";
        public const string RESOLVED = "Resolved";
        public const string CLOSED = "Closed";
        public const string CANCELLED = "Cancelled";
    }
}
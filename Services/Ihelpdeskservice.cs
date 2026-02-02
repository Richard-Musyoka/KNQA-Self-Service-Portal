using KNQASelfService.Models;

namespace KNQASelfService.Services
{
    /// <summary>
    /// Service interface for Help Desk Ticket operations
    /// </summary>
    public interface IHelpDeskService
    {
        /// <summary>
        /// Get all help desk tickets for a specific employee
        /// </summary>
        /// <param name="employeeNo">Employee number</param>
        /// <returns>List of help desk tickets</returns>
        Task<List<HelpDeskTicket>> GetTicketsByEmployeeAsync(string employeeNo);

        /// <summary>
        /// Get all help desk tickets (admin view)
        /// </summary>
        /// <returns>List of all help desk tickets</returns>
        Task<List<HelpDeskTicket>> GetAllTicketsAsync();

        /// <summary>
        /// Get a specific help desk ticket by ticket number
        /// </summary>
        /// <param name="ticketNo">Ticket number</param>
        /// <returns>Help desk ticket or null if not found</returns>
        Task<HelpDeskTicket?> GetTicketAsync(string ticketNo);

        /// <summary>
        /// Create a new help desk ticket
        /// </summary>
        /// <param name="ticket">Ticket creation model</param>
        /// <returns>Success message or error</returns>
        Task<string> CreateTicketAsync(HelpDeskTicketCreate ticket);

        /// <summary>
        /// Update an existing help desk ticket
        /// </summary>
        /// <param name="ticket">Updated ticket</param>
        /// <returns>Success message or error</returns>
        Task<string> UpdateTicketAsync(HelpDeskTicket ticket);

        /// <summary>
        /// Delete a help desk ticket
        /// </summary>
        /// <param name="ticketNo">Ticket number</param>
        /// <param name="etag">ETag for concurrency control</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteTicketAsync(string ticketNo, string etag);

        /// <summary>
        /// Get summary statistics for help desk tickets
        /// </summary>
        /// <param name="employeeNo">Employee number (optional, for employee-specific stats)</param>
        /// <returns>Ticket summary statistics</returns>
        Task<HelpDeskTicketSummary> GetTicketSummaryAsync(string? employeeNo = null);

        /// <summary>
        /// Get available ticket categories
        /// </summary>
        /// <returns>List of categories</returns>
        Task<List<string>> GetTicketCategoriesAsync();

        /// <summary>
        /// Get available locations
        /// </summary>
        /// <returns>List of locations</returns>
        Task<List<string>> GetLocationsAsync();
    }
}
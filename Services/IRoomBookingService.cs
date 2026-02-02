using KNQASelfService.Models;

namespace KNQASelfService.Services
{
    public interface IRoomBookingService
    {
        Task<List<AvailableMeetingRoom>> GetAvailableRoomsAsync(string? date = null, string? startTime = null, string? endTime = null);
        Task<List<RoomBooking>> GetBookingsByEmployeeAsync(string employeeNo);
        Task<List<RoomBooking>> GetAllBookingsAsync();
        Task<RoomBooking?> GetBookingAsync(string bookingNo);
        Task<string> CreateBookingAsync(RoomBookingCreate booking);
        Task<string> UpdateBookingAsync(RoomBooking booking);
        Task<bool> DeleteBookingAsync(string bookingNo, string etag);
        Task<RoomBookingSummary> GetBookingSummaryAsync(string? employeeNo = null);
        Task<List<string>> GetRoomLocationsAsync();
        Task<bool> CheckRoomAvailabilityAsync(string roomNo, string date, string startTime, string endTime);
        Task<string> CancelBookingAsync(string bookingNo, string remarks = "");
        Task<List<MeetingParticipant>> GetBookingParticipantsAsync(string bookingNo);
        Task<List<Employee>> SearchEmployeesAsync(string searchTerm);
        Task<Employee?> GetEmployeeByNoAsync(string employeeNo);
        Task<string> AddParticipantAsync(MeetingParticipantCreate participant);
        Task<string> UpdateParticipantAsync(MeetingParticipant participant);
        Task<bool> RemoveParticipantAsync(string participantNo, string etag);
        Task<string> SendInvitationAsync(string bookingNo);
        Task<string> SendReminderAsync(string bookingNo);

    }
}
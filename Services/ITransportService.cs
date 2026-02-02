using KNQASelfService.Models;

namespace KNQASelfService.Services
{
    public interface ITransportService
    {
        Task<List<FleetVehicle>> GetAvailableVehiclesAsync(string? date = null, string? startTime = null, string? endTime = null);
        Task<List<TransportRequest>> GetRequestsByEmployeeAsync(string employeeNo);
        Task<List<TransportRequest>> GetAllRequestsAsync();
        Task<TransportRequest?> GetRequestAsync(string requestNo);
        Task<string> CreateRequestAsync(TransportRequestCreate request);
        Task<string> UpdateRequestAsync(TransportRequest request);
        Task<bool> DeleteRequestAsync(string requestNo, string etag);
        Task<TransportRequestSummary> GetRequestSummaryAsync(string? employeeNo = null);
        Task<bool> CheckVehicleAvailabilityAsync(string vehicleNo, string date, string startTime, string endTime);
        Task<string> CancelRequestAsync(string requestNo, string remarks = "");

        // Travelling Employees
        Task<List<TravellingEmployee>> GetTravellingEmployeesAsync(string requestNo);
        Task<List<Employee>> SearchEmployeesForTravelAsync(string searchTerm);
        Task<string> AddTravellingEmployeeAsync(TravellingEmployeeCreate employee);
        Task<string> UpdateTravellingEmployeeAsync(TravellingEmployee employee);
        Task<bool> RemoveTravellingEmployeeAsync(string employeeNo, string requestNo, string etag);
        Task<List<Employee>> GetDriversAsync();
        Task<Employee?> GetDriverByNoAsync(string driverNo);
    }
}
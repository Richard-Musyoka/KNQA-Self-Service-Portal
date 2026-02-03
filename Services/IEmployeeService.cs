using KNQASelfService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KNQASelfService.Services
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetActiveEmployeesAsync();
        Task<Employee?> GetEmployeeAsync(string employeeNo);
        Task<Employee?> GetCurrentEmployeeAsync();
        Task<List<Employee>> GetEmployeesByDepartmentAsync(string departmentCode);
        Task<List<Employee>> GetSubordinatesAsync(string managerEmployeeNo);
        Task<List<Employee>> GetPotentialAppraisersAsync();
        Task<List<Employee>> GetPotentialManagersAsync();
    }
}
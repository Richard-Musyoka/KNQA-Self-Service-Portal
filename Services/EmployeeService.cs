using KNQASelfService.Models;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace KNQASelfService.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmployeeService> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly string _baseUrl;
        private readonly string _authHeaderValue;

        public EmployeeService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<EmployeeService> logger,
            ICurrentUserService currentUserService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _currentUserService = currentUserService;

            _baseUrl = _configuration["BusinessCentral:BaseUrl"] ?? "";
            var username = _configuration["BusinessCentral:Username"] ?? "";
            var password = _configuration["BusinessCentral:Password"] ?? "";

            _authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        }

        private string GetFullUrl(string endpoint)
        {
            return $"{_baseUrl}/{endpoint}";
        }

        private void SetupRequestHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<Employee>> GetActiveEmployeesAsync()
        {
            try
            {
                var url = GetFullUrl("Company('KNQA')/Employees?$filter=Status eq 'Active'&$select=No,FullName,FirstName,LastName,JobTitle,DepartmentCode,Email,Status,ManagerNo&$orderby=FullName");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<Employee>>(content);

                    if (result?.Value != null)
                    {
                        // Ensure FullName is set
                        foreach (var emp in result.Value)
                        {
                            if (string.IsNullOrEmpty(emp.FullName))
                            {
                                emp.FullName = $"{emp.FirstName} {emp.LastName}".Trim();
                            }
                        }

                        return result.Value;
                    }
                }

                return new List<Employee>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching active employees");
                return new List<Employee>();
            }
        }

        public async Task<Employee?> GetEmployeeAsync(string employeeNo)
        {
            try
            {
                var url = GetFullUrl($"Company('KNQA')/Employees('{employeeNo}')?$select=No,FullName,FirstName,LastName,JobTitle,DepartmentCode,Email,Status,ManagerNo,EmploymentDate");

                SetupRequestHeaders();

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var employee = JsonConvert.DeserializeObject<Employee>(content);

                    // Ensure FullName is set
                    if (employee != null && string.IsNullOrEmpty(employee.FullName))
                    {
                        employee.FullName = $"{employee.FirstName} {employee.LastName}".Trim();
                    }

                    return employee;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching employee {employeeNo}");
                return null;
            }
        }

        public async Task<Employee?> GetCurrentEmployeeAsync()
        {
            var employeeNo = await _currentUserService.GetEmployeeNoAsync();
            if (string.IsNullOrEmpty(employeeNo))
                return null;

            return await GetEmployeeAsync(employeeNo);
        }

        public async Task<List<Employee>> GetEmployeesByDepartmentAsync(string departmentCode)
        {
            try
            {
                var allEmployees = await GetActiveEmployeesAsync();
                return allEmployees
                    .Where(e => e.DepartmentCode == departmentCode)
                    .OrderBy(e => e.FullName)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching employees for department {departmentCode}");
                return new List<Employee>();
            }
        }

        public async Task<List<Employee>> GetSubordinatesAsync(string managerEmployeeNo)
        {
            try
            {
                var allEmployees = await GetActiveEmployeesAsync();
                return allEmployees
                    .Where(e => e.ManagerNo == managerEmployeeNo)
                    .OrderBy(e => e.FullName)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching subordinates for manager {managerEmployeeNo}");
                return new List<Employee>();
            }
        }

        public async Task<List<Employee>> GetPotentialManagersAsync()
        {
            try
            {
                var allEmployees = await GetActiveEmployeesAsync();

                // Filter employees who have managerial roles or are supervisors
                return allEmployees
                    .Where(e => e.JobTitle?.ToLower().Contains("manager") == true ||
                               e.JobTitle?.ToLower().Contains("supervisor") == true ||
                               e.JobTitle?.ToLower().Contains("head") == true ||
                               e.JobTitle?.ToLower().Contains("director") == true)
                    .OrderBy(e => e.FullName)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching potential managers");
                return new List<Employee>();
            }
        }

        public async Task<List<Employee>> GetPotentialAppraisersAsync()
        {
            try
            {
                // Get all active employees
                var allEmployees = await GetActiveEmployeesAsync();

                // Get current user to exclude themselves from appraiser list
                var currentEmployee = await GetCurrentEmployeeAsync();
                var currentEmployeeNo = currentEmployee?.No;

                // Define criteria for potential appraisers
                // 1. Anyone who is a manager/supervisor
                var managersAndSupervisors = allEmployees
                    .Where(e => e.JobTitle != null &&
                           (e.JobTitle.Contains("Manager", StringComparison.OrdinalIgnoreCase) ||
                            e.JobTitle.Contains("Supervisor", StringComparison.OrdinalIgnoreCase) ||
                            e.JobTitle.Contains("Lead", StringComparison.OrdinalIgnoreCase) ||
                            e.JobTitle.Contains("Head", StringComparison.OrdinalIgnoreCase) ||
                            e.JobTitle.Contains("Director", StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                // 2. Department heads (people with no manager or specific designation)
                var departmentHeads = allEmployees
                    .Where(e => string.IsNullOrEmpty(e.ManagerNo) ||
                           e.JobTitle?.Contains("Head", StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                // 3. Senior staff with experience
                var seniorStaff = allEmployees
                    .Where(e => e.JobTitle != null &&
                           (e.JobTitle.Contains("Senior", StringComparison.OrdinalIgnoreCase) ||
                            e.JobTitle.Contains("Principal", StringComparison.OrdinalIgnoreCase) ||
                            e.JobTitle.Contains("Chief", StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                // Combine all potential appraisers
                var potentialAppraisers = managersAndSupervisors
                    .Union(departmentHeads)
                    .Union(seniorStaff)
                    .DistinctBy(e => e.No)
                    .Where(e => e.No != currentEmployeeNo) // Exclude self
                    .OrderBy(e => e.FullName)
                    .ToList();

                // If no specific appraisers found, return all active employees except self
                if (!potentialAppraisers.Any())
                {
                    potentialAppraisers = allEmployees
                        .Where(e => e.No != currentEmployeeNo)
                        .OrderBy(e => e.FullName)
                        .Take(50) // Limit results
                        .ToList();
                }

                return potentialAppraisers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching potential appraisers");
                return new List<Employee>();
            }
        }

        public async Task<List<Employee>> GetPotentialAppraisersAsync(string employeeNo)
        {
            try
            {
                // Get the employee details to understand their context
                var employee = await GetEmployeeAsync(employeeNo);
                if (employee == null)
                {
                    _logger.LogWarning($"Employee {employeeNo} not found");
                    return await GetPotentialAppraisersAsync(); // Return generic list
                }

                // Get all potential appraisers
                var allPotentialAppraisers = await GetPotentialAppraisersAsync();

                // Filter based on employee's department
                if (!string.IsNullOrEmpty(employee.DepartmentCode))
                {
                    // Include appraisers from same department
                    var departmentAppraisers = allPotentialAppraisers
                        .Where(a => a.DepartmentCode == employee.DepartmentCode)
                        .ToList();

                    // Also include the employee's manager if they have one
                    if (!string.IsNullOrEmpty(employee.ManagerNo))
                    {
                        var manager = await GetEmployeeAsync(employee.ManagerNo);
                        if (manager != null && !departmentAppraisers.Any(a => a.No == manager.No))
                        {
                            departmentAppraisers.Add(manager);
                        }
                    }

                    // Include higher-level managers
                    var seniorManagers = allPotentialAppraisers
                        .Where(a => a.JobTitle?.Contains("Senior Manager", StringComparison.OrdinalIgnoreCase) == true ||
                                   a.JobTitle?.Contains("Director", StringComparison.OrdinalIgnoreCase) == true ||
                                   a.JobTitle?.Contains("Head", StringComparison.OrdinalIgnoreCase) == true)
                        .ToList();

                    var combinedList = departmentAppraisers
                        .Union(seniorManagers)
                        .DistinctBy(e => e.No)
                        .Where(e => e.No != employeeNo) // Exclude self
                        .OrderBy(e => e.FullName)
                        .ToList();

                    return combinedList.Any() ? combinedList : allPotentialAppraisers;
                }

                // If no department, return all potential appraisers excluding self
                return allPotentialAppraisers
                    .Where(e => e.No != employeeNo)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching potential appraisers for employee {employeeNo}");
                return await GetPotentialAppraisersAsync(); // Return generic list as fallback
            }
        }

        public async Task<bool> IsEmployeeEligibleForAppraisal(string employeeNo)
        {
            try
            {
                var employee = await GetEmployeeAsync(employeeNo);
                if (employee == null)
                    return false;

                // Check if employee is active
                if (employee.Status != "Active")
                    return false;

                // Check if employee has been employed long enough (e.g., at least 3 months)
                if (employee.EmploymentDate.HasValue)
                {
                    var threeMonthsAgo = DateTime.Now.AddMonths(-3);
                    if (employee.EmploymentDate > threeMonthsAgo)
                        return false;
                }

                // Additional business rules can be added here
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error checking appraisal eligibility for employee {employeeNo}");
                return false;
            }
        }

        public async Task<List<Employee>> SearchEmployeesAsync(string searchTerm)
        {
            try
            {
                var allEmployees = await GetActiveEmployeesAsync();

                if (string.IsNullOrWhiteSpace(searchTerm))
                    return allEmployees.Take(20).ToList();

                var searchTermLower = searchTerm.ToLower();
                return allEmployees
                    .Where(e =>
                        (!string.IsNullOrEmpty(e.FullName) && e.FullName.ToLower().Contains(searchTermLower)) ||
                        (!string.IsNullOrEmpty(e.No) && e.No.ToLower().Contains(searchTermLower)) ||
                        (!string.IsNullOrEmpty(e.Email) && e.Email.ToLower().Contains(searchTermLower)) ||
                        (!string.IsNullOrEmpty(e.JobTitle) && e.JobTitle.ToLower().Contains(searchTermLower)))
                    .Take(20)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error searching employees with term: {searchTerm}");
                return new List<Employee>();
            }
        }

        public async Task<List<Employee>> GetDepartmentHeadsAsync()
        {
            try
            {
                var allEmployees = await GetActiveEmployeesAsync();

                // Get unique departments
                var departments = allEmployees
                    .Where(e => !string.IsNullOrEmpty(e.DepartmentCode))
                    .Select(e => e.DepartmentCode)
                    .Distinct()
                    .ToList();

                var departmentHeads = new List<Employee>();

                foreach (var dept in departments)
                {
                    // Find potential department head (person with "Head" in title or no manager)
                    var potentialHead = allEmployees
                        .Where(e => e.DepartmentCode == dept)
                        .OrderByDescending(e =>
                            (e.JobTitle?.Contains("Head", StringComparison.OrdinalIgnoreCase) == true ? 3 : 0) +
                            (e.JobTitle?.Contains("Manager", StringComparison.OrdinalIgnoreCase) == true ? 2 : 0) +
                            (string.IsNullOrEmpty(e.ManagerNo) ? 1 : 0))
                        .FirstOrDefault();

                    if (potentialHead != null)
                    {
                        departmentHeads.Add(potentialHead);
                    }
                }

                return departmentHeads
                    .DistinctBy(e => e.No)
                    .OrderBy(e => e.DepartmentCode)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching department heads");
                return new List<Employee>();
            }
        }

        private async Task<bool> IsDepartmentHead(string employeeNo)
        {
            try
            {
                var employee = await GetEmployeeAsync(employeeNo);
                if (employee == null) return false;

                // Check if this employee is a department head
                var departmentHeads = await GetDepartmentHeadsAsync();
                return departmentHeads.Any(dh => dh.No == employeeNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error checking if employee {employeeNo} is department head");
                return false;
            }
        }

        private class ODataResponse<T>
        {
            [JsonProperty("@odata.context")]
            public string? Context { get; set; }

            [JsonProperty("value")]
            public List<T>? Value { get; set; }
        }
    }
}
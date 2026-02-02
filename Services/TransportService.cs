using KNQASelfService.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KNQASelfService.Services
{
    public class TransportService : ITransportService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TransportService> _logger;
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;

        public TransportService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<TransportService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _baseUrl = _configuration["BusinessCentral:BaseUrl"] ?? "";
            _username = _configuration["BusinessCentral:Username"] ?? "";
            _password = _configuration["BusinessCentral:Password"] ?? "";

            SetupHttpClient();
        }

        private void SetupHttpClient()
        {
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_username}:{_password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<FleetVehicle>> GetAvailableVehiclesAsync(string? date = null, string? startTime = null, string? endTime = null)
        {
            try
            {
                _logger.LogInformation($"Fetching available vehicles. Date: {date}, Start: {startTime}, End: {endTime}");

                var url = $"{_baseUrl}/Fleet_List?$filter=Status eq '{VehicleStatus.AVAILABLE}'";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch available vehicles. Status: {response.StatusCode}");
                    return new List<FleetVehicle>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<FleetVehicle>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Value ?? new List<FleetVehicle>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available vehicles");
                return new List<FleetVehicle>();
            }
        }

        public async Task<List<TransportRequest>> GetRequestsByEmployeeAsync(string employeeNo)
        {
            try
            {
                _logger.LogInformation($"Fetching transport requests for employee: {employeeNo}");

                var url = $"{_baseUrl}/Transport_Requests?$filter=Employee_No eq '{employeeNo}'&$orderby=Request_Date desc, Start_Time desc";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch requests. Status: {response.StatusCode}");
                    return new List<TransportRequest>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<TransportRequest>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Value ?? new List<TransportRequest>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching requests for employee {employeeNo}");
                return new List<TransportRequest>();
            }
        }

        public async Task<List<TransportRequest>> GetAllRequestsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all transport requests");

                var url = $"{_baseUrl}/Transport_Requests?$orderby=Request_Date desc, Start_Time desc";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch all requests. Status: {response.StatusCode}");
                    return new List<TransportRequest>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<TransportRequest>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Value ?? new List<TransportRequest>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all requests");
                return new List<TransportRequest>();
            }
        }

        public async Task<TransportRequest?> GetRequestAsync(string requestNo)
        {
            try
            {
                _logger.LogInformation($"Fetching transport request: {requestNo}");

                var url = $"{_baseUrl}/Transport_Requests('{requestNo}')";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch request {requestNo}. Status: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TransportRequest>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching request {requestNo}");
                return null;
            }
        }

        public async Task<string> CreateRequestAsync(TransportRequestCreate request)
        {
            try
            {
                _logger.LogInformation($"Creating transport request for employee: {request.EmployeeNo}");

                var url = $"{_baseUrl}/Transport_Requests";
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var createdRequest = JsonSerializer.Deserialize<TransportRequest>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation($"Request created successfully: {createdRequest?.RequestNo}");
                    return $"Success: Request {createdRequest?.RequestNo} created successfully";
                }
                else
                {
                    _logger.LogError($"Failed to create request. Status: {response.StatusCode}, Response: {responseContent}");

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<BCErrorResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return $"Error: {errorResponse?.Error?.Message ?? responseContent}";
                    }
                    catch
                    {
                        return $"Error: {responseContent}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transport request");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateRequestAsync(TransportRequest request)
        {
            try
            {
                _logger.LogInformation($"Updating transport request: {request.RequestNo}");

                var url = $"{_baseUrl}/Transport_Requests('{request.RequestNo}')";
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!string.IsNullOrEmpty(request.ODataEtag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", request.ODataEtag);
                }

                var response = await _httpClient.PatchAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Request {request.RequestNo} updated successfully");
                    return $"Success: Request {request.RequestNo} updated successfully";
                }
                else
                {
                    _logger.LogError($"Failed to update request. Status: {response.StatusCode}, Response: {responseContent}");

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<BCErrorResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return $"Error: {errorResponse?.Error?.Message ?? responseContent}";
                    }
                    catch
                    {
                        return $"Error: {responseContent}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating request {request.RequestNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<bool> DeleteRequestAsync(string requestNo, string etag)
        {
            try
            {
                _logger.LogInformation($"Deleting transport request: {requestNo}");

                var url = $"{_baseUrl}/Transport_Requests('{requestNo}')";

                if (!string.IsNullOrEmpty(etag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Request {requestNo} deleted successfully");
                    return true;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to delete request. Status: {response.StatusCode}, Response: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting request {requestNo}");
                return false;
            }
        }

        public async Task<TransportRequestSummary> GetRequestSummaryAsync(string? employeeNo = null)
        {
            try
            {
                var requests = string.IsNullOrEmpty(employeeNo)
                    ? await GetAllRequestsAsync()
                    : await GetRequestsByEmployeeAsync(employeeNo);

                return new TransportRequestSummary
                {
                    TotalRequests = requests.Count,
                    PendingRequests = requests.Count(r => r.Status == TransportStatus.PENDING),
                    ApprovedRequests = requests.Count(r => r.Status == TransportStatus.APPROVED),
                    RejectedRequests = requests.Count(r => r.Status == TransportStatus.REJECTED),
                    CompletedRequests = requests.Count(r => r.Status == TransportStatus.COMPLETED),
                    InProgressRequests = requests.Count(r => r.Status == TransportStatus.IN_PROGRESS)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating request summary");
                return new TransportRequestSummary();
            }
        }

        public async Task<bool> CheckVehicleAvailabilityAsync(string vehicleNo, string date, string startTime, string endTime)
        {
            try
            {
                var url = $"{_baseUrl}/Transport_Requests?$filter=Vehicle_Allocated eq '{vehicleNo}' " +
                         $"and Trip_Planned_Start_Date eq '{date}' " +
                         $"and ((Start_Time le '{startTime}' and Return_Time ge '{startTime}') " +
                         $"or (Start_Time le '{endTime}' and Return_Time ge '{endTime}') " +
                         $"or (Start_Time ge '{startTime}' and Return_Time le '{endTime}')) " +
                         $"and Status ne '{TransportStatus.CANCELLED}'";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to check vehicle availability. Status: {response.StatusCode}");
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<TransportRequest>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Vehicle is available if no overlapping requests found
                return !(result?.Value?.Any() ?? false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking vehicle availability");
                return false;
            }
        }

        public async Task<string> CancelRequestAsync(string requestNo, string remarks = "")
        {
            try
            {
                var request = await GetRequestAsync(requestNo);
                if (request == null)
                    return "Error: Request not found";

                if (request.Status == TransportStatus.CANCELLED)
                    return "Request is already cancelled";

                if (request.Status == TransportStatus.COMPLETED)
                    return "Cannot cancel a completed request";

                // Update request status to cancelled
                request.Status = TransportStatus.CANCELLED;
                return await UpdateRequestAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling request {requestNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<List<TravellingEmployee>> GetTravellingEmployeesAsync(string requestNo)
        {
            try
            {
                _logger.LogInformation($"Fetching travelling employees for request: {requestNo}");

                var url = $"{_baseUrl}/Travelling_Employees?$filter=Request_No eq '{requestNo}'";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch travelling employees. Status: {response.StatusCode}");
                    return new List<TravellingEmployee>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<TravellingEmployee>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Value ?? new List<TravellingEmployee>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching travelling employees for request {requestNo}");
                return new List<TravellingEmployee>();
            }
        }

        public async Task<List<Employee>> SearchEmployeesForTravelAsync(string searchTerm)
        {
            try
            {
                _logger.LogInformation($"Searching employees for travel: {searchTerm}");

                var url = $"{_baseUrl}/Employees?$filter=contains(FullName, '{searchTerm}') or contains(No, '{searchTerm}') " +
                         $"&$top=20&$select=No,FullName,First_Name,Last_Name,Status,Job_Title,Global_Dimension_1_Code,E_Mail";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to search employees. Status: {response.StatusCode}");
                    return new List<Employee>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<Employee>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Value ?? new List<Employee>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching employees for travel");
                return new List<Employee>();
            }
        }

        public async Task<string> AddTravellingEmployeeAsync(TravellingEmployeeCreate employee)
        {
            try
            {
                _logger.LogInformation($"Adding travelling employee to request: {employee.RequestNo}");

                var url = $"{_baseUrl}/Travelling_Employees";
                var json = JsonSerializer.Serialize(employee, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Travelling employee added successfully");
                    return "Success: Travelling employee added successfully";
                }
                else
                {
                    _logger.LogError($"Failed to add travelling employee. Status: {response.StatusCode}, Response: {responseContent}");

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<BCErrorResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return $"Error: {errorResponse?.Error?.Message ?? responseContent}";
                    }
                    catch
                    {
                        return $"Error: {responseContent}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding travelling employee");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateTravellingEmployeeAsync(TravellingEmployee employee)
        {
            try
            {
                _logger.LogInformation($"Updating travelling employee: {employee.EmployeeNo}");

                var url = $"{_baseUrl}/Travelling_Employees('{employee.EmployeeNo}')";
                var json = JsonSerializer.Serialize(employee, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!string.IsNullOrEmpty(employee.ODataEtag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", employee.ODataEtag);
                }

                var response = await _httpClient.PatchAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Travelling employee {employee.EmployeeNo} updated successfully");
                    return $"Success: Travelling employee updated successfully";
                }
                else
                {
                    _logger.LogError($"Failed to update travelling employee. Status: {response.StatusCode}, Response: {responseContent}");

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<BCErrorResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return $"Error: {errorResponse?.Error?.Message ?? responseContent}";
                    }
                    catch
                    {
                        return $"Error: {responseContent}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating travelling employee {employee.EmployeeNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<bool> RemoveTravellingEmployeeAsync(string employeeNo, string requestNo, string etag)
        {
            try
            {
                _logger.LogInformation($"Removing travelling employee: {employeeNo} from request: {requestNo}");

                // Note: You might need to adjust this URL based on your BC API structure
                var url = $"{_baseUrl}/Travelling_Employees('{employeeNo}')";

                if (!string.IsNullOrEmpty(etag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Travelling employee {employeeNo} removed successfully");
                    return true;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to remove travelling employee. Status: {response.StatusCode}, Response: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing travelling employee {employeeNo}");
                return false;
            }
        }

        public async Task<List<Employee>> GetDriversAsync()
        {
            try
            {
                _logger.LogInformation("Fetching drivers");

                // Assuming drivers are employees with a specific job title or role
                // Adjust the filter as per your business logic
                var url = $"{_baseUrl}/Employees?$filter=contains(Job_Title, 'Driver') or contains(Job_Title, 'Chauffeur') " +
                         $"&$top=50&$select=No,FullName,First_Name,Last_Name,Status,Job_Title,Global_Dimension_1_Code,E_Mail";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch drivers. Status: {response.StatusCode}");
                    return new List<Employee>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<Employee>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Value ?? new List<Employee>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching drivers");
                return new List<Employee>();
            }
        }

        public async Task<Employee?> GetDriverByNoAsync(string driverNo)
        {
            try
            {
                _logger.LogInformation($"Fetching driver: {driverNo}");

                var url = $"{_baseUrl}/Employees('{driverNo}')?$select=No,FullName,First_Name,Last_Name,Status,Job_Title,Global_Dimension_1_Code,E_Mail";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch driver {driverNo}. Status: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Employee>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching driver {driverNo}");
                return null;
            }
        }

        // Helper class for OData responses
        private class ODataResponse<T>
        {
            public List<T>? Value { get; set; }
        }

        // Helper class for BC error responses
        private class BCErrorResponse
        {
            public BCError? Error { get; set; }
        }

        private class BCError
        {
            public string? Code { get; set; }
            public string? Message { get; set; }
        }
    }
}
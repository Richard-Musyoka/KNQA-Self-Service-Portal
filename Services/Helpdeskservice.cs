using KNQASelfService.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KNQASelfService.Services
{
    /// <summary>
    /// Service implementation for Help Desk Ticket operations
    /// </summary>
    public class HelpDeskService : IHelpDeskService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HelpDeskService> _logger;
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;

        public HelpDeskService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<HelpDeskService> logger)
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

        public async Task<List<HelpDeskTicket>> GetTicketsByEmployeeAsync(string employeeNo)
        {
            try
            {
                _logger.LogInformation($"Fetching help desk tickets for employee: {employeeNo}");

                var url = $"{_baseUrl}/HelpDeskTickets?$filter=EmployeeNo eq '{employeeNo}'";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch tickets. Status: {response.StatusCode}");
                    return new List<HelpDeskTicket>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<HelpDeskTicket>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Value ?? new List<HelpDeskTicket>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching tickets for employee {employeeNo}");
                return new List<HelpDeskTicket>();
            }
        }

        public async Task<List<HelpDeskTicket>> GetAllTicketsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all help desk tickets");

                var url = $"{_baseUrl}/HelpDeskTickets";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch tickets. Status: {response.StatusCode}");
                    return new List<HelpDeskTicket>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<HelpDeskTicket>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Value ?? new List<HelpDeskTicket>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all tickets");
                return new List<HelpDeskTicket>();
            }
        }

        public async Task<HelpDeskTicket?> GetTicketAsync(string ticketNo)
        {
            try
            {
                _logger.LogInformation($"Fetching ticket: {ticketNo}");

                var url = $"{_baseUrl}/HelpDeskTickets('{ticketNo}')";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch ticket {ticketNo}. Status: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<HelpDeskTicket>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching ticket {ticketNo}");
                return null;
            }
        }

        public async Task<string> CreateTicketAsync(HelpDeskTicketCreate ticket)
        {
            try
            {
                _logger.LogInformation($"Creating help desk ticket for employee: {ticket.EmployeeNo}");

                var url = $"{_baseUrl}/HelpDeskTickets";
                var json = JsonSerializer.Serialize(ticket);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var createdTicket = JsonSerializer.Deserialize<HelpDeskTicket>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation($"Ticket created successfully: {createdTicket?.TicketNo}");
                    return $"Success: Ticket {createdTicket?.TicketNo} created successfully";
                }
                else
                {
                    _logger.LogError($"Failed to create ticket. Status: {response.StatusCode}, Response: {responseContent}");

                    // Try to parse error message
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
                _logger.LogError(ex, "Error creating ticket");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateTicketAsync(HelpDeskTicket ticket)
        {
            try
            {
                _logger.LogInformation($"Updating ticket: {ticket.TicketNo}");

                var url = $"{_baseUrl}/HelpDeskTickets('{ticket.TicketNo}')";
                var json = JsonSerializer.Serialize(ticket);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Add If-Match header for concurrency control
                if (!string.IsNullOrEmpty(ticket.ODataEtag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", ticket.ODataEtag);
                }

                var response = await _httpClient.PatchAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Ticket {ticket.TicketNo} updated successfully");
                    return $"Success: Ticket {ticket.TicketNo} updated successfully";
                }
                else
                {
                    _logger.LogError($"Failed to update ticket. Status: {response.StatusCode}, Response: {responseContent}");

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
                _logger.LogError(ex, $"Error updating ticket {ticket.TicketNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<bool> DeleteTicketAsync(string ticketNo, string etag)
        {
            try
            {
                _logger.LogInformation($"Deleting ticket: {ticketNo}");

                var url = $"{_baseUrl}/HelpDeskTickets('{ticketNo}')";

                // Add If-Match header for concurrency control
                if (!string.IsNullOrEmpty(etag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Ticket {ticketNo} deleted successfully");
                    return true;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to delete ticket. Status: {response.StatusCode}, Response: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting ticket {ticketNo}");
                return false;
            }
        }

        public async Task<HelpDeskTicketSummary> GetTicketSummaryAsync(string? employeeNo = null)
        {
            try
            {
                var tickets = string.IsNullOrEmpty(employeeNo)
                    ? await GetAllTicketsAsync()
                    : await GetTicketsByEmployeeAsync(employeeNo);

                return new HelpDeskTicketSummary
                {
                    TotalTickets = tickets.Count,
                    OpenTickets = tickets.Count(t => t.Status == TicketStatus.OPEN),
                    InProgressTickets = tickets.Count(t => t.Status == TicketStatus.IN_PROGRESS),
                    ResolvedTickets = tickets.Count(t => t.Status == TicketStatus.RESOLVED),
                    ClosedTickets = tickets.Count(t => t.Status == TicketStatus.CLOSED),
                    HighPriorityTickets = tickets.Count(t => t.Priority == TicketPriority.HIGH || t.Priority == TicketPriority.URGENT),
                    MediumPriorityTickets = tickets.Count(t => t.Priority == TicketPriority.MEDIUM),
                    LowPriorityTickets = tickets.Count(t => t.Priority == TicketPriority.LOW)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating ticket summary");
                return new HelpDeskTicketSummary();
            }
        }

        public async Task<List<string>> GetTicketCategoriesAsync()
        {
            // In a real implementation, this might fetch from BC
            // For now, returning predefined categories
            return await Task.FromResult(new List<string>
            {
                TicketCategory.IT_SUPPORT,
                TicketCategory.HR_INQUIRY,
                TicketCategory.FINANCE,
                TicketCategory.FACILITIES,
                TicketCategory.PAYROLL,
                TicketCategory.BENEFITS,
                TicketCategory.EQUIPMENT,
                TicketCategory.ACCESS,
                TicketCategory.GENERAL,
                TicketCategory.OTHER
            });
        }

        public async Task<List<string>> GetLocationsAsync()
        {
            try
            {
                // This would typically fetch from a Locations API endpoint
                // For now, returning sample locations
                return await Task.FromResult(new List<string>
                {
                    "Head Office",
                    "Branch Office",
                    "Remote",
                    "Site A",
                    "Site B",
                    "Warehouse"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching locations");
                return new List<string>();
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
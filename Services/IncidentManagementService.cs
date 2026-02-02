// IncidentManagementService.cs
using KNQASelfService.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace KNQASelfService.Services
{
    public class IncidentManagementService : IIncidentManagementService
    {
        private readonly HttpClient _httpClient;
        private readonly BusinessCentralSettings _settings;
        private readonly ILogger<IncidentManagementService> _logger;
        private readonly string _authHeaderValue;

        public IncidentManagementService(
            IConfiguration configuration,
            ILogger<IncidentManagementService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();

            _settings = new BusinessCentralSettings();
            configuration.GetSection("BusinessCentral").Bind(_settings);

            var credentials = $"{_settings.Username}:{_settings.Password}";
            _authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            _logger.LogInformation($"✅ Incident Management Service initialized: {_settings.BaseUrl}");
        }

        #region Helper Methods
        private string GetFullUrl(string endpoint)
        {
            return $"{_settings.BaseUrl}/Company('{_settings.CompanyName}')/{endpoint}";
        }

        private void SetupHttpHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private string ParseBusinessCentralError(string errorResponse)
        {
            try
            {
                if (string.IsNullOrEmpty(errorResponse))
                    return "Unknown error from Business Central";

                if (errorResponse.Trim().StartsWith("{"))
                {
                    using (var doc = System.Text.Json.JsonDocument.Parse(errorResponse))
                    {
                        if (doc.RootElement.TryGetProperty("error", out var errorElement))
                        {
                            if (errorElement.TryGetProperty("message", out var messageElement))
                            {
                                var message = messageElement.GetString();
                                if (!string.IsNullOrEmpty(message))
                                {
                                    var correlationIndex = message.IndexOf("CorrelationId:");
                                    if (correlationIndex > 0)
                                    {
                                        return message.Substring(0, correlationIndex).Trim();
                                    }
                                    return message;
                                }
                            }
                        }
                    }
                }

                return errorResponse.Length > 500 ? errorResponse.Substring(0, 500) + "..." : errorResponse;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error parsing BC error: {ex.Message}");
                return errorResponse.Length > 200 ? errorResponse.Substring(0, 200) + "..." : errorResponse;
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(Func<HttpClient, Task<HttpResponseMessage>> requestFunc)
        {
            try
            {
                var response = await requestFunc(_httpClient);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request failed");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request failed");
                throw;
            }
        }
        #endregion

        #region READ Operations
        public async Task<List<IncidentManagement>> GetIncidentsByEmployeeAsync(string employeeNo)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeNo))
                {
                    _logger.LogWarning("Employee number is required");
                    return new List<IncidentManagement>();
                }

                var encodedEmployeeNo = Uri.EscapeDataString(employeeNo);
                var url = GetFullUrl($"Incident_Management_List?$filter=Employee_No eq '{encodedEmployeeNo}'&$orderby=Incident_Date desc");

                _logger.LogInformation($"📡 GET Incidents for Employee: {employeeNo}");

                SetupHttpHeaders();
                var response = await SendRequestAsync(async client => await client.GetAsync(url));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<IncidentManagementODataResponse>(content);

                    _logger.LogInformation($"✅ Retrieved {result?.Value?.Count ?? 0} incidents for {employeeNo}");
                    return result?.Value ?? new List<IncidentManagement>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Status: {response.StatusCode}, Error: {error}");
                    return new List<IncidentManagement>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting incidents for employee {employeeNo}");
                return new List<IncidentManagement>();
            }
        }

        public async Task<IncidentManagement> GetIncidentAsync(string incidentReference)
        {
            try
            {
                if (string.IsNullOrEmpty(incidentReference))
                {
                    _logger.LogWarning("Incident reference is required");
                    return null;
                }

                var encodedReference = Uri.EscapeDataString(incidentReference);
                var url = GetFullUrl($"Incident_Management_List?$filter=Incident_Reference eq '{encodedReference}'");

                _logger.LogInformation($"📡 GET Incident: {incidentReference}");

                SetupHttpHeaders();
                var response = await SendRequestAsync(async client => await client.GetAsync(url));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<IncidentManagementODataResponse>(content);
                    var incident = result?.Value?.FirstOrDefault();

                    if (incident != null)
                    {
                        _logger.LogInformation($"✅ Retrieved incident {incidentReference}");
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ Incident {incidentReference} not found");
                    }

                    return incident;
                }

                _logger.LogWarning($"⚠️ Incident {incidentReference} not found (HTTP {response.StatusCode})");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting incident {incidentReference}");
                return null;
            }
        }

        public async Task<List<IncidentManagement>> GetIncidentsAsync(IncidentManagementFilter filter)
        {
            try
            {
                var filterParts = new List<string>();

                if (!string.IsNullOrEmpty(filter.EmployeeNo))
                    filterParts.Add($"Employee_No eq '{Uri.EscapeDataString(filter.EmployeeNo)}'");

                if (!string.IsNullOrEmpty(filter.Department))
                    filterParts.Add($"Department eq '{Uri.EscapeDataString(filter.Department)}'");

                if (!string.IsNullOrEmpty(filter.IncidentStatus))
                    filterParts.Add($"Incident_Status eq '{Uri.EscapeDataString(filter.IncidentStatus)}'");

                if (!string.IsNullOrEmpty(filter.IncidentType))
                    filterParts.Add($"Incident_Type eq '{Uri.EscapeDataString(filter.IncidentType)}'");

                if (filter.FromDate.HasValue)
                    filterParts.Add($"Incident_Date ge {filter.FromDate.Value:yyyy-MM-dd}");

                if (filter.ToDate.HasValue)
                    filterParts.Add($"Incident_Date le {filter.ToDate.Value:yyyy-MM-dd}");

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    var encodedSearchTerm = Uri.EscapeDataString(filter.SearchTerm);
                    filterParts.Add($"(contains(Incident_Description, '{encodedSearchTerm}') or contains(Incident_Reference, '{encodedSearchTerm}') or contains(Employee_Name, '{encodedSearchTerm}'))");
                }

                var filterQuery = filterParts.Count > 0 ? $"$filter={string.Join(" and ", filterParts)}" : "";
                var orderBy = !string.IsNullOrEmpty(filter.OrderBy) ? $"&$orderby={Uri.EscapeDataString(filter.OrderBy)}" : "";

                var url = GetFullUrl($"Incident_Management_List?{filterQuery}{orderBy}");

                _logger.LogInformation($"📡 GET Incidents with filters: {url}");

                SetupHttpHeaders();
                var response = await SendRequestAsync(async client => await client.GetAsync(url));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<IncidentManagementODataResponse>(content);

                    var incidents = result?.Value ?? new List<IncidentManagement>();
                    _logger.LogInformation($"✅ Retrieved {incidents.Count} incidents with filters");

                    return incidents;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ GET Incidents failed: {response.StatusCode}, Error: {error}");
                    return new List<IncidentManagement>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting incidents with filters");
                return new List<IncidentManagement>();
            }
        }

        public async Task<List<IncidentManagement>> GetIncidentsByStatusAsync(string employeeNo, string status)
        {
            var filter = new IncidentManagementFilter
            {
                EmployeeNo = employeeNo,
                IncidentStatus = status
            };
            return await GetIncidentsAsync(filter);
        }

        public async Task<IncidentManagementSummary> GetIncidentSummaryAsync(string employeeNo)
        {
            try
            {
                var filter = new IncidentManagementFilter { EmployeeNo = employeeNo };
                var incidents = await GetIncidentsAsync(filter);

                var summary = new IncidentManagementSummary
                {
                    TotalIncidents = incidents.Count,
                    OpenIncidents = incidents.Count(i => i.IncidentStatus == "Open"),
                    InProgressIncidents = incidents.Count(i => i.IncidentStatus == "In Progress"),
                    ResolvedIncidents = incidents.Count(i => i.IncidentStatus == "Resolved"),
                    ClosedIncidents = incidents.Count(i => i.IncidentStatus == "Closed")
                };

                _logger.LogInformation($"✅ Generated incident summary for {employeeNo}: {summary.TotalIncidents} incidents");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error generating incident summary for {employeeNo}");
                return new IncidentManagementSummary();
            }
        }
        #endregion

        #region CREATE Operations
        public async Task<(bool Success, string Message, IncidentManagement Data)> CreateIncidentAsync(IncidentManagementCreate model)
        {
            try
            {
                _logger.LogInformation($"🎯 CREATE: Starting new incident creation for {model.EmployeeNo}");

                // Validate required fields
                var validationErrors = await ValidateIncidentAsync(model);
                if (validationErrors.Any())
                {
                    var errorMessage = string.Join("; ", validationErrors);
                    _logger.LogWarning($"❌ Validation failed: {errorMessage}");
                    return (false, errorMessage, null);
                }

                var url = GetFullUrl("Incident_Management_Application");

                // Parse and validate date
                if (!DateTime.TryParse(model.IncidentDate, out var incidentDate))
                {
                    return (false, "Error: Invalid Incident Date format. Use YYYY-MM-DD format.", null);
                }

                _logger.LogInformation($"📤 Creating incident for Employee: {model.EmployeeName}, Type: {model.IncidentType}");

                // Prepare payload
                var payload = new
                {
                    Employee_No = model.EmployeeNo,
                    Employee_Name = model.EmployeeName,
                    Job_Title = model.JobTitle,
                    Department = model.Department,
                    Incident_Description = model.IncidentDescription,
                    Incident_Date = incidentDate.ToString("yyyy-MM-dd"),
                    Incident_Time = model.IncidentTime,
                    Incidence_Location_Name = model.IncidenceLocationName,
                    Incident_Type = model.IncidentType,
                    Incident_Status = "Open"
                };

                var jsonContent = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-dd"
                });

                _logger.LogInformation($"📤 Payload: {jsonContent}");

                SetupHttpHeaders();

                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await SendRequestAsync(async client => await client.PostAsync(url, content));

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"✅ Incident created successfully!");

                    try
                    {
                        var createdIncident = JsonConvert.DeserializeObject<IncidentManagement>(responseContent);
                        return (true, "Incident reported successfully", createdIncident);
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning($"⚠️ Could not parse created incident: {parseEx.Message}");
                        return (true, "Incident reported successfully (details not retrieved)", null);
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Status Code: {response.StatusCode}");
                    _logger.LogError($"❌ Response: {errorResponse}");

                    var errorMessage = ParseBusinessCentralError(errorResponse);
                    return (false, errorMessage, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception creating incident");
                return (false, $"Error creating incident: {ex.Message}", null);
            }
        }
        #endregion

        #region UPDATE Operations
        public async Task<(bool Success, string Message)> UpdateIncidentAsync(IncidentManagementUpdate model, string etag)
        {
            try
            {
                _logger.LogInformation($"🔄 UPDATE: Updating incident {model.IncidentReference}");

                if (string.IsNullOrEmpty(model.IncidentReference))
                    return (false, "Error: Incident reference is required");

                var url = GetFullUrl($"Incident_Management_Application('{model.IncidentReference}')");
                _logger.LogInformation($"📡 Update URL: {url}");

                using var requestClient = new HttpClient();
                SetupHttpHeadersForUpdate(requestClient, etag);

                // Prepare update payload
                var updatePayload = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(model.IncidentDescription))
                    updatePayload["Incident_Description"] = model.IncidentDescription;

                if (!string.IsNullOrEmpty(model.IncidenceLocationName))
                    updatePayload["Incidence_Location_Name"] = model.IncidenceLocationName;

                if (!string.IsNullOrEmpty(model.IncidentType))
                    updatePayload["Incident_Type"] = model.IncidentType;

                var jsonContent = JsonConvert.SerializeObject(updatePayload, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-dd"
                });

                _logger.LogInformation($"📤 Update Payload: {jsonContent}");

                var request = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                var response = await requestClient.SendAsync(request);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation($"✅ Incident {model.IncidentReference} updated successfully");
                    return (true, "Incident updated successfully");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                {
                    _logger.LogError($"❌ ETag mismatch - record modified by another user");
                    return (false, "Error: This record was modified by another user. Please refresh and try again.");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError($"❌ Incident not found: {model.IncidentReference}");
                    return (false, "Error: Incident not found or may have been deleted");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Update failed. Status: {response.StatusCode}");
                    _logger.LogError($"❌ Error: {errorResponse}");

                    var errorMessage = ParseBusinessCentralError(errorResponse);
                    return (false, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating incident");
                return (false, $"Error updating incident: {ex.Message}");
            }
        }

        private void SetupHttpHeadersForUpdate(HttpClient client, string etag)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authHeaderValue);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(etag))
            {
                client.DefaultRequestHeaders.Add("If-Match", etag);
            }
        }
        #endregion

        #region DELETE Operations
        public async Task<bool> DeleteIncidentAsync(string incidentReference, string etag)
        {
            try
            {
                var url = GetFullUrl($"Incident_Management_Application('{incidentReference}')");

                _logger.LogInformation($"🗑️ DELETE: {incidentReference}");

                SetupHttpHeaders();

                if (!string.IsNullOrEmpty(etag))
                {
                    _httpClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                var response = await SendRequestAsync(async client => await client.DeleteAsync(url));

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation($"✅ Incident {incidentReference} deleted successfully");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Delete failed: {response.StatusCode}, Error: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting incident {incidentReference}");
                return false;
            }
        }
        #endregion

        #region VALIDATION Operations
        public async Task<List<string>> ValidateIncidentAsync(IncidentManagementCreate model)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(model.EmployeeNo))
                errors.Add("Employee number is required");

            if (string.IsNullOrEmpty(model.EmployeeName))
                errors.Add("Employee name is required");

            if (string.IsNullOrEmpty(model.IncidentDescription))
                errors.Add("Incident description is required");
            else if (model.IncidentDescription.Length < 20)
                errors.Add("Incident description must be at least 20 characters");

            if (string.IsNullOrEmpty(model.IncidentDate))
                errors.Add("Incident date is required");
            else if (!DateTime.TryParse(model.IncidentDate, out _))
                errors.Add("Invalid incident date format. Use YYYY-MM-DD format.");

            if (string.IsNullOrEmpty(model.IncidenceLocationName))
                errors.Add("Incident location is required");

            if (string.IsNullOrEmpty(model.IncidentType))
                errors.Add("Incident type is required");

            return await Task.FromResult(errors);
        }

        public async Task<bool> IncidentExistsAsync(string incidentReference)
        {
            try
            {
                var incident = await GetIncidentAsync(incidentReference);
                return incident != null;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
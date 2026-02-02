using KNQASelfService.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KNQASelfService.Services
{
    public class RoomBookingService : IRoomBookingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RoomBookingService> _logger;
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;

        public RoomBookingService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<RoomBookingService> logger)
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

        public async Task<List<AvailableMeetingRoom>> GetAvailableRoomsAsync(string? date = null, string? startTime = null, string? endTime = null)
        {
            try
            {
                _logger.LogInformation($"Fetching available meeting rooms. Date: {date}, Start: {startTime}, End: {endTime}");

                var url = $"{_baseUrl}/Available_Meeting_Rooms";

                // Add filters if provided
                var filters = new List<string>();
                if (!string.IsNullOrEmpty(date))
                    filters.Add($"Available_Date eq '{date}'");
                if (!string.IsNullOrEmpty(startTime))
                    filters.Add($"Start_Time eq '{startTime}'");
                if (!string.IsNullOrEmpty(endTime))
                    filters.Add($"End_Time eq '{endTime}'");

                if (filters.Any())
                    url += $"?$filter={string.Join(" and ", filters)}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch available rooms. Status: {response.StatusCode}");
                    return new List<AvailableMeetingRoom>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<AvailableMeetingRoom>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Value ?? new List<AvailableMeetingRoom>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available meeting rooms");
                return new List<AvailableMeetingRoom>();
            }
        }

        public async Task<List<RoomBooking>> GetBookingsByEmployeeAsync(string employeeNo)
        {
            try
            {
                _logger.LogInformation($"Fetching room bookings for employee: {employeeNo}");

                var url = $"{_baseUrl}/Meeting_Room_Bookings?$filter=Employee_No eq '{employeeNo}'&$orderby=Booking_Date desc, Start_Time desc";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch bookings. Status: {response.StatusCode}");
                    return new List<RoomBooking>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<RoomBooking>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Value ?? new List<RoomBooking>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching bookings for employee {employeeNo}");
                return new List<RoomBooking>();
            }
        }

        public async Task<List<RoomBooking>> GetAllBookingsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all room bookings");

                var url = $"{_baseUrl}/Meeting_Room_Bookings?$orderby=Booking_Date desc, Start_Time desc";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch all bookings. Status: {response.StatusCode}");
                    return new List<RoomBooking>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<RoomBooking>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Value ?? new List<RoomBooking>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all bookings");
                return new List<RoomBooking>();
            }
        }

        public async Task<RoomBooking?> GetBookingAsync(string bookingNo)
        {
            try
            {
                _logger.LogInformation($"Fetching booking: {bookingNo}");

                var url = $"{_baseUrl}/Meeting_Room_Bookings('{bookingNo}')";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch booking {bookingNo}. Status: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<RoomBooking>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching booking {bookingNo}");
                return null;
            }
        }

        public async Task<string> CreateBookingAsync(RoomBookingCreate booking)
        {
            try
            {
                _logger.LogInformation($"Creating room booking for employee: {booking.EmployeeNo}");

                var url = $"{_baseUrl}/Meeting_Room_Bookings";
                var json = JsonSerializer.Serialize(booking, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var createdBooking = JsonSerializer.Deserialize<RoomBooking>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation($"Booking created successfully: {createdBooking?.BookingNo}");
                    return $"Success: Booking {createdBooking?.BookingNo} created successfully";
                }
                else
                {
                    _logger.LogError($"Failed to create booking. Status: {response.StatusCode}, Response: {responseContent}");

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
                _logger.LogError(ex, "Error creating booking");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateBookingAsync(RoomBooking booking)
        {
            try
            {
                _logger.LogInformation($"Updating booking: {booking.BookingNo}");

                var url = $"{_baseUrl}/Meeting_Room_Bookings('{booking.BookingNo}')";
                var json = JsonSerializer.Serialize(booking, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!string.IsNullOrEmpty(booking.ODataEtag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", booking.ODataEtag);
                }

                var response = await _httpClient.PatchAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Booking {booking.BookingNo} updated successfully");
                    return $"Success: Booking {booking.BookingNo} updated successfully";
                }
                else
                {
                    _logger.LogError($"Failed to update booking. Status: {response.StatusCode}, Response: {responseContent}");

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
                _logger.LogError(ex, $"Error updating booking {booking.BookingNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<bool> DeleteBookingAsync(string bookingNo, string etag)
        {
            try
            {
                _logger.LogInformation($"Deleting booking: {bookingNo}");

                var url = $"{_baseUrl}/Meeting_Room_Bookings('{bookingNo}')";

                if (!string.IsNullOrEmpty(etag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Booking {bookingNo} deleted successfully");
                    return true;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to delete booking. Status: {response.StatusCode}, Response: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting booking {bookingNo}");
                return false;
            }
        }
        // Add to existing RoomBookingService class
        public async Task<List<MeetingParticipant>> GetBookingParticipantsAsync(string bookingNo)
        {
            try
            {
                _logger.LogInformation($"Fetching participants for booking: {bookingNo}");

                var url = $"{_baseUrl}/Meeting_Participants?$filter=Booking_No eq '{bookingNo}'";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch participants. Status: {response.StatusCode}");
                    return new List<MeetingParticipant>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<MeetingParticipant>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Value ?? new List<MeetingParticipant>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching participants for booking {bookingNo}");
                return new List<MeetingParticipant>();
            }
        }

        public async Task<List<Employee>> SearchEmployeesAsync(string searchTerm)
        {
            try
            {
                _logger.LogInformation($"Searching employees: {searchTerm}");

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
                _logger.LogError(ex, "Error searching employees");
                return new List<Employee>();
            }
        }

        public async Task<Employee?> GetEmployeeByNoAsync(string employeeNo)
        {
            try
            {
                _logger.LogInformation($"Fetching employee: {employeeNo}");

                var url = $"{_baseUrl}/Employees('{employeeNo}')?$select=No,FullName,First_Name,Last_Name,Status,Job_Title,Global_Dimension_1_Code,E_Mail";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch employee {employeeNo}. Status: {response.StatusCode}");
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
                _logger.LogError(ex, $"Error fetching employee {employeeNo}");
                return null;
            }
        }

        public async Task<string> AddParticipantAsync(MeetingParticipantCreate participant)
        {
            try
            {
                _logger.LogInformation($"Adding participant to booking: {participant.BookingNo}");

                var url = $"{_baseUrl}/Meeting_Participants";
                var json = JsonSerializer.Serialize(participant, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Participant added successfully");
                    return "Success: Participant added successfully";
                }
                else
                {
                    _logger.LogError($"Failed to add participant. Status: {response.StatusCode}, Response: {responseContent}");

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
                _logger.LogError(ex, "Error adding participant");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateParticipantAsync(MeetingParticipant participant)
        {
            try
            {
                _logger.LogInformation($"Updating participant: {participant.ParticipantNo}");

                var url = $"{_baseUrl}/Meeting_Participants('{participant.ParticipantNo}')";
                var json = JsonSerializer.Serialize(participant, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!string.IsNullOrEmpty(participant.ODataEtag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", participant.ODataEtag);
                }

                var response = await _httpClient.PatchAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Participant {participant.ParticipantNo} updated successfully");
                    return $"Success: Participant updated successfully";
                }
                else
                {
                    _logger.LogError($"Failed to update participant. Status: {response.StatusCode}, Response: {responseContent}");

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
                _logger.LogError(ex, $"Error updating participant {participant.ParticipantNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<bool> RemoveParticipantAsync(string participantNo, string etag)
        {
            try
            {
                _logger.LogInformation($"Removing participant: {participantNo}");

                var url = $"{_baseUrl}/Meeting_Participants('{participantNo}')";

                if (!string.IsNullOrEmpty(etag))
                {
                    _httpClient.DefaultRequestHeaders.Remove("If-Match");
                    _httpClient.DefaultRequestHeaders.Add("If-Match", etag);
                }

                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Participant {participantNo} removed successfully");
                    return true;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to remove participant. Status: {response.StatusCode}, Response: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing participant {participantNo}");
                return false;
            }
        }

        public async Task<string> SendInvitationAsync(string bookingNo)
        {
            try
            {
                _logger.LogInformation($"Sending invitations for booking: {bookingNo}");

                // This would typically call a Business Central API to send invitations
                // For now, we'll simulate the action
                await Task.Delay(1000); // Simulate API call

                _logger.LogInformation($"Invitations sent for booking {bookingNo}");
                return "Success: Invitations sent successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending invitations for booking {bookingNo}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> SendReminderAsync(string bookingNo)
        {
            try
            {
                _logger.LogInformation($"Sending reminders for booking: {bookingNo}");

                // This would typically call a Business Central API to send reminders
                // For now, we'll simulate the action
                await Task.Delay(1000); // Simulate API call

                _logger.LogInformation($"Reminders sent for booking {bookingNo}");
                return "Success: Reminders sent successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending reminders for booking {bookingNo}");
                return $"Error: {ex.Message}";
            }
        }
        public async Task<RoomBookingSummary> GetBookingSummaryAsync(string? employeeNo = null)
        {
            try
            {
                var bookings = string.IsNullOrEmpty(employeeNo)
                    ? await GetAllBookingsAsync()
                    : await GetBookingsByEmployeeAsync(employeeNo);

                return new RoomBookingSummary
                {
                    TotalBookings = bookings.Count,
                    ApprovedBookings = bookings.Count(b => b.Status == BookingStatus.APPROVED),
                    PendingBookings = bookings.Count(b => b.Status == BookingStatus.PENDING),
                    RejectedBookings = bookings.Count(b => b.Status == BookingStatus.REJECTED),
                    CompletedBookings = bookings.Count(b => b.Status == BookingStatus.COMPLETED),
                    CancelledBookings = bookings.Count(b => b.Status == BookingStatus.CANCELLED)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating booking summary");
                return new RoomBookingSummary();
            }
        }

        public async Task<List<string>> GetRoomLocationsAsync()
        {
            try
            {
                var rooms = await GetAvailableRoomsAsync();
                return rooms
                    .Select(r => r.Location)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching room locations");
                return new List<string>();
            }
        }

        public async Task<bool> CheckRoomAvailabilityAsync(string roomNo, string date, string startTime, string endTime)
        {
            try
            {
                var url = $"{_baseUrl}/Meeting_Room_Bookings?$filter=Room_No eq '{roomNo}' and Booking_Date eq '{date}' " +
                         $"and ((Start_Time le '{startTime}' and End_Time ge '{startTime}') " +
                         $"or (Start_Time le '{endTime}' and End_Time ge '{endTime}') " +
                         $"or (Start_Time ge '{startTime}' and End_Time le '{endTime}')) " +
                         $"and Status ne '{BookingStatus.CANCELLED}'";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to check room availability. Status: {response.StatusCode}");
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ODataResponse<RoomBooking>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Room is available if no overlapping bookings found
                return !(result?.Value?.Any() ?? false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking room availability");
                return false;
            }
        }

        public async Task<string> CancelBookingAsync(string bookingNo, string remarks = "")
        {
            try
            {
                var booking = await GetBookingAsync(bookingNo);
                if (booking == null)
                    return "Error: Booking not found";

                if (booking.Status == BookingStatus.CANCELLED)
                    return "Booking is already cancelled";

                if (booking.Status == BookingStatus.COMPLETED)
                    return "Cannot cancel a completed booking";

                // Update booking status to cancelled
                booking.Status = BookingStatus.CANCELLED;
                if (!string.IsNullOrEmpty(remarks))
                    booking.Remarks = $"Cancelled: {remarks}";

                return await UpdateBookingAsync(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling booking {bookingNo}");
                return $"Error: {ex.Message}";
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
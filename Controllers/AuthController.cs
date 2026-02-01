using KNQASelfService.Models;
using KNQASelfService.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KNQASelfService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserAuthService _authService;
        private readonly IBusinessCentralService _bcService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserAuthService authService,
            IBusinessCentralService bcService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _bcService = bcService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel login)
        {
            try
            {
                _logger.LogInformation($"🔐 Login attempt for: {login.Email}");

                // Authenticate user with your local database
                var user = await _authService.AuthenticateAsync(login.Email, login.Password);
                if (user == null)
                {
                    _logger.LogWarning($"❌ Invalid credentials for: {login.Email}");
                    return Unauthorized("Invalid credentials");
                }

                _logger.LogInformation($"✅ User authenticated: {user.Email} (UserNameId: {user.UserNameId})");

                // Get employee number from local database
                string employeeNo = user.EmployeeNo ?? string.Empty;
                string employeeName = user.FullName;
                string department = string.Empty;

                _logger.LogInformation($"📋 Employee info from database - EmployeeNo: {employeeNo}, Name: {employeeName}");

                // Optionally verify the employee exists in Business Central and get additional details
                if (!string.IsNullOrEmpty(employeeNo))
                {
                    try
                    {
                        var bcEmployee = await _bcService.GetEmployeeByNoAsync(employeeNo);
                        if (bcEmployee != null)
                        {
                            _logger.LogInformation($"✅ Employee verified in BC: {bcEmployee.FullName}");
                            // Optionally use BC employee name if you prefer
                            // employeeName = bcEmployee.FullName;
                            department = bcEmployee.DepartmentCode ?? string.Empty;
                        }
                        else
                        {
                            _logger.LogWarning($"⚠️ Employee {employeeNo} not found in Business Central");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"⚠️ Could not verify employee in BC: {ex.Message}");
                        // Continue with login anyway
                    }
                }
                else
                {
                    _logger.LogWarning($"⚠️ No employee number in database for user {user.Email}");
                }

                // Create claims for the authenticated user
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim("FullName", employeeName),
            new Claim("Email", user.Email),
            new Claim(ClaimTypes.Role, user.RoleId.ToString())
        };

                // Add employee-related claims if we have valid data
                if (!string.IsNullOrEmpty(employeeNo))
                {
                    claims.Add(new Claim("EmployeeNo", employeeNo));
                    claims.Add(new Claim("EmployeeNumber", employeeNo)); // Alternative claim name
                }

                if (!string.IsNullOrEmpty(department))
                {
                    claims.Add(new Claim("Department", department));
                }

                // Log all claims being added
                _logger.LogInformation("🎫 Creating claims:");
                foreach (var claim in claims)
                {
                    _logger.LogInformation($"   - {claim.Type}: {claim.Value}");
                }

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    });

                _logger.LogInformation($"✅ Login successful for: {user.Email}");

                return Ok(new
                {
                    success = true,
                    employeeNo = employeeNo,
                    employeeName = employeeName,
                    hasEmployeeNo = !string.IsNullOrEmpty(employeeNo)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Login error for {login.Email}");
                return StatusCode(500, "An error occurred during login");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation("✅ User logged out successfully");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during logout");
                return StatusCode(500, "An error occurred during logout");
            }
        }

        [HttpGet("check")]
        public IActionResult CheckAuth()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
                return Ok(new
                {
                    authenticated = true,
                    username = User.Identity.Name,
                    claims = claims
                });
            }

            return Ok(new { authenticated = false });
        }
    }
}
namespace KNQASelfService.Interfaces.UserManagement
{
    public interface IEmailService
    {
        Task SendNewUserEmailAsync(string toEmail, string fullName, string email, string password);
        Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink);
        Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode);
    }
}
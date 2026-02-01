namespace KNQASelfService.Interfaces
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(string email);
        Task<bool> ValidateOtpAsync(string email, string otpCode);
        Task CleanupExpiredOtpsAsync();
    }
}
using KNQASelfService.Interfaces;
using KNQASelfService.Models;
using KNQASelfService.Context;
using Microsoft.EntityFrameworkCore;

namespace KNQASelfService.Services
{
    public class OtpService : IOtpService
    {
        private readonly AppDbContext _context;
        private const int OTP_LENGTH = 6;
        private const int OTP_EXPIRY_MINUTES = 5;

        public OtpService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateOtpAsync(string email)
        {
            // Generate random 6-digit OTP
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();

            // Invalidate any existing OTPs for this email
            var existingOtps = await _context.OtpVerifications
                .Where(o => o.Email == email && !o.IsUsed)
                .ToListAsync();

            foreach (var otp in existingOtps)
            {
                otp.IsUsed = true;
            }

            // Create new OTP
            var otpVerification = new OtpVerification
            {
                Email = email,
                OtpCode = otpCode,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(OTP_EXPIRY_MINUTES),
                IsUsed = false
            };

            _context.OtpVerifications.Add(otpVerification);
            await _context.SaveChangesAsync();

            return otpCode;
        }

        public async Task<bool> ValidateOtpAsync(string email, string otpCode)
        {
            var otp = await _context.OtpVerifications
                .Where(o => o.Email == email && o.OtpCode == otpCode && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
                return false;

            if (otp.ExpiresAt < DateTime.Now)
            {
                // OTP has expired
                otp.IsUsed = true;
                await _context.SaveChangesAsync();
                return false;
            }

            // Mark OTP as used
            otp.IsUsed = true;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task CleanupExpiredOtpsAsync()
        {
            var expiredDate = DateTime.Now.AddDays(-1);
            var expiredOtps = await _context.OtpVerifications
                .Where(o => o.CreatedAt < expiredDate)
                .ToListAsync();

            _context.OtpVerifications.RemoveRange(expiredOtps);
            await _context.SaveChangesAsync();
        }
    }
}
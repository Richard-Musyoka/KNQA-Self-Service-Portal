using System.Net;
using System.Net.Mail;
using KNQASelfService.Interfaces;
using KNQASelfService.Interfaces.UserManagement;

namespace KNQASelfService.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendNewUserEmailAsync(string toEmail, string fullName, string email, string password)
        {
            var subject = "Welcome to KNQA Self-Service Portal";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #00286E 0%, #003d8f 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
                        .credentials {{ background: white; padding: 20px; border-left: 4px solid #D4A853; margin: 20px 0; }}
                        .credential-item {{ margin: 10px 0; }}
                        .credential-label {{ font-weight: bold; color: #00286E; }}
                        .credential-value {{ color: #333; background: #f0f0f0; padding: 5px 10px; border-radius: 4px; display: inline-block; }}
                        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
                        .button {{ background-color: #D4A853; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Welcome to KNQA!</h1>
                            <p>Kenya National Qualifications Authority</p>
                        </div>
                        <div class='content'>
                            <h2>Hello {fullName},</h2>
                            <p>Your account has been successfully created on the KNQA Self-Service Portal. Below are your login credentials:</p>
                            
                            <div class='credentials'>
                                <div class='credential-item'>
                                    <span class='credential-label'>Email:</span><br/>
                                    <span class='credential-value'>{email}</span>
                                </div>
                                <div class='credential-item'>
                                    <span class='credential-label'>Password:</span><br/>
                                    <span class='credential-value'>{password}</span>
                                </div>
                            </div>

                            <p><strong>Important:</strong> For security reasons, please change your password after your first login.</p>
                            
                            <div style='text-align: center;'>
                                <a href='{_configuration["AppSettings:BaseUrl"]}/login' class='button'>Login Now</a>
                            </div>

                            <p>If you have any questions or need assistance, please contact our support team.</p>
                            
                            <p>Best regards,<br/>
                            KNQA Self-Service Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.</p>
                            <p>&copy; 2025 Kenya National Qualifications Authority. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }
        public async Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
        {
            var subject = "Your Login OTP - KNQA Self-Service Portal";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #00286E 0%, #003d8f 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
                        .otp-box {{ background: white; padding: 20px; border-left: 4px solid #D4A853; margin: 20px 0; text-align: center; }}
                        .otp-code {{ font-size: 32px; font-weight: bold; color: #00286E; letter-spacing: 8px; background: #f0f0f0; padding: 15px; border-radius: 8px; display: inline-block; }}
                        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
                        .warning {{ background: #fff3cd; border-left: 4px solid #ff9800; padding: 15px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Login Verification</h1>
                            <p>Kenya National Qualifications Authority</p>
                        </div>
                        <div class='content'>
                            <h2>Hello {fullName},</h2>
                            <p>You have requested to log in to your KNQA Self-Service account. Please use the following One-Time Password (OTP) to complete your login:</p>
                    
                            <div class='otp-box'>
                                <p style='margin: 0; color: #666; font-size: 14px;'>Your OTP Code:</p>
                                <div class='otp-code'>{otpCode}</div>
                                <p style='margin: 10px 0 0 0; color: #999; font-size: 12px;'>This code will expire in 5 minutes</p>
                            </div>

                            <div class='warning'>
                                <strong>⚠️ Security Notice:</strong><br/>
                                • Do not share this code with anyone<br/>
                                • KNQA will never ask for your OTP via phone or email<br/>
                                • If you didn't request this code, please ignore this email
                            </div>
                    
                            <p>Best regards,<br/>
                            KNQA Self-Service Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.</p>
                            <p>&copy; 2025 Kenya National Qualifications Authority. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink)
        {
            var subject = "Password Reset Request - KNQA Self-Service Portal";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #00286E 0%, #003d8f 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
                        .button {{ background-color: #D4A853; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
                        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Password Reset Request</h1>
                        </div>
                        <div class='content'>
                            <h2>Hello {fullName},</h2>
                            <p>We received a request to reset your password. Click the button below to reset it:</p>
                            
                            <div style='text-align: center;'>
                                <a href='{resetLink}' class='button'>Reset Password</a>
                            </div>

                            <p>If you didn't request this, please ignore this email. Your password will remain unchanged.</p>
                            <p>This link will expire in 24 hours.</p>
                            
                            <p>Best regards,<br/>
                            KNQA Self-Service Team</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(username, password)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                // Use SendMailAsync instead of SendAsync
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log the error (you can inject ILogger here)
                Console.WriteLine($"Email sending failed: {ex.Message}");
                throw;
            }
        }
    }
}
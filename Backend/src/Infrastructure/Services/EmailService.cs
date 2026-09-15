using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendOtpEmailAsync(string email, string code, OtpPurpose purpose, CancellationToken ct = default)
        {
            var purposeText = purpose switch
            {
                OtpPurpose.EmailVerification => "Email Verification",
                OtpPurpose.PasswordReset => "Password Reset",
                OtpPurpose.TwoFactor => "Two-Factor Authentication",
                _ => "Authentication"
            };

            _logger.LogInformation("==================================================");
            _logger.LogInformation("EMAIL DISPATCH -> To: {Email} | Purpose: {Purpose}", email, purposeText);
            _logger.LogInformation("SECURITY CODE: {Code} (Expires in 10 minutes)", code);
            _logger.LogInformation("==================================================");

            // In production, an SMTP client or transactional email provider (SendGrid, Mailgun, AWS SES) would send here.
            return Task.CompletedTask;
        }
    }
}

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers
{
    [EnableRateLimiting("auth-policy")]
    public class AuthController : ApiControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            var result = await Mediator.Send(command);
            return MatchResult(result, new { message = "User registered successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var (ip, userAgent) = GetClientInfo();
            var enrichedCommand = command with
            {
                IpAddress = command.IpAddress ?? ip,
                UserAgent = command.UserAgent ?? userAgent
            };

            var result = await Mediator.Send(enrichedCommand);
            return MatchResult(result);
        }

        [HttpPost("login/verify-2fa")]
        public async Task<IActionResult> VerifyLogin2fa([FromBody] VerifyLogin2faCommand command)
        {
            var (ip, userAgent) = GetClientInfo();
            var enrichedCommand = command with
            {
                IpAddress = command.IpAddress ?? ip,
                UserAgent = command.UserAgent ?? userAgent
            };

            var result = await Mediator.Send(enrichedCommand);
            return MatchResult(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var (ip, userAgent) = GetClientInfo();
            var command = new RefreshTokenCommand(
                AccessToken: request.AccessToken,
                RefreshToken: request.RefreshToken,
                IpAddress: ip,
                UserAgent: userAgent);

            var result = await Mediator.Send(command);
            return MatchResult(result);
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var (ip, userAgent) = GetClientInfo();
            var command = new GoogleLoginCommand(
                IdToken: request.IdToken,
                IpAddress: ip,
                UserAgent: userAgent);

            var result = await Mediator.Send(command);
            return MatchResult(result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
        {
            var result = await Mediator.Send(command);
            return MatchResult(result, new { message = "If the email is registered, a password reset code has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            var result = await Mediator.Send(command);
            return MatchResult(result, new { message = "Password reset successfully. You may now log in with your new password." });
        }

        [HttpPost("verification/send-otp")]
        public async Task<IActionResult> SendEmailVerificationOtp([FromBody] SendEmailOtpCommand command)
        {
            var result = await Mediator.Send(command);
            return MatchResult(result, new { message = "If the email is valid, a verification code has been dispatched." });
        }

        [HttpPost("verification/verify-otp")]
        public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyEmailOtpCommand command)
        {
            var result = await Mediator.Send(command);
            return MatchResult(result, new { message = "Email verified successfully." });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var command = new ChangePasswordCommand(userId.Value, request.CurrentPassword, request.NewPassword);
            var result = await Mediator.Send(command);
            return MatchResult(result, new { message = "Password changed successfully. All sessions have been logged out." });
        }

        [Authorize]
        [HttpPost("2fa/enable")]
        public async Task<IActionResult> Enable2fa()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await Mediator.Send(new Enable2faCommand(userId.Value));
            return MatchResult(result);
        }

        [Authorize]
        [HttpPost("2fa/verify")]
        public async Task<IActionResult> Verify2fa([FromBody] Verify2faRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await Mediator.Send(new Verify2faCommand(userId.Value, request.Code));
            return MatchResult(result, new { message = "Two-factor authentication enabled successfully." });
        }

        [Authorize]
        [HttpPost("2fa/disable")]
        public async Task<IActionResult> Disable2fa([FromBody] Verify2faRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await Mediator.Send(new Disable2faCommand(userId.Value, request.Code));
            return MatchResult(result, new { message = "Two-factor authentication disabled successfully." });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var result = await Mediator.Send(new LogoutCommand(request.RefreshToken));
            return MatchResult(result, new { message = "Logged out successfully." });
        }

        private int? GetCurrentUserId()
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            return int.TryParse(claimValue, out var id) ? id : null;
        }

        private (string? IpAddress, string? UserAgent) GetClientInfo()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) && !string.IsNullOrWhiteSpace(forwarded))
            {
                ip = forwarded.ToString().Split(',')[0].Trim();
            }

            var userAgent = Request.Headers.UserAgent.ToString();
            return (ip, string.IsNullOrWhiteSpace(userAgent) ? null : userAgent);
        }
    }
}

using System;
using MediatR;
using Shared;

namespace Application.Features.Auth
{
    public record AuthResponse(
        string? Token,
        string? RefreshToken,
        string? Email,
        string? FullName,
        bool Requires2fa = false,
        string? Message = null
    );

    public record VerifyLogin2faCommand(
        string Email,
        string Code,
        string? IpAddress = null,
        string? UserAgent = null
    ) : IRequest<Result<AuthResponse>>;

    public record RefreshTokenRequest(string? AccessToken, string RefreshToken);

    public record RefreshTokenCommand(
        string? AccessToken,
        string RefreshToken,
        string? IpAddress = null,
        string? UserAgent = null
    ) : IRequest<Result<AuthResponse>>;

    public record ForgotPasswordCommand(string Email) : IRequest<Result>;

    public record ResetPasswordCommand(
        string Email,
        string Code,
        string NewPassword
    ) : IRequest<Result>;

    public record ChangePasswordRequest(
        string CurrentPassword,
        string NewPassword
    );

    public record ChangePasswordCommand(
        int UserId,
        string CurrentPassword,
        string NewPassword
    ) : IRequest<Result>;

    public record SendEmailOtpCommand(string Email) : IRequest<Result>;

    public record VerifyEmailOtpCommand(string Email, string Code) : IRequest<Result>;

    public record GoogleLoginRequest(string IdToken);

    public record GoogleLoginCommand(
        string IdToken,
        string? IpAddress = null,
        string? UserAgent = null
    ) : IRequest<Result<AuthResponse>>;

    public record Enable2faResponse(string Secret, string QrCodeUri);

    public record Enable2faCommand(int UserId) : IRequest<Result<Enable2faResponse>>;

    public record Verify2faRequest(string Code);

    public record Verify2faCommand(int UserId, string Code) : IRequest<Result>;

    public record Disable2faCommand(int UserId, string Code) : IRequest<Result>;

    public record LogoutRequest(string RefreshToken);

    public record LogoutCommand(string RefreshToken) : IRequest<Result>;
}

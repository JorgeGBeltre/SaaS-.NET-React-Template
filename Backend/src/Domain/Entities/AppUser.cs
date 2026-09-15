using System;
using System.Collections.Generic;
using Domain.Common;

namespace Domain.Entities
{
    public class AppUser : BaseEntity
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; } = [];
        public byte[] PasswordSalt { get; set; } = [];

        // Security & Account Protection
        public int FailedLoginAttempts { get; set; } = 0;
        public bool IsLocked { get; set; } = false;
        public DateTime? LockedUntil { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? LastLoginIp { get; set; }

        // Email Verification
        public bool EmailVerified { get; set; } = false;
        public DateTime? EmailVerifiedAt { get; set; }

        // 2FA / TOTP (Authenticator App)
        public string? TotpSecret { get; set; }
        public bool TotpEnabled { get; set; } = false;

        // OAuth2 Google
        public string? GoogleId { get; set; }

        // Navigation properties
        public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
        public virtual ICollection<OtpCode> OtpCodes { get; set; } = new List<OtpCode>();
    }
}

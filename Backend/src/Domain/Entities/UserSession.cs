using System;
using Domain.Common;

namespace Domain.Entities
{
    public class UserSession : BaseEntity
    {
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public string RefreshTokenHash { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime ExpiresAt { get; set; }
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    }
}

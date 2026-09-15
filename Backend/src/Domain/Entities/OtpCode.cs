using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class OtpCode : BaseEntity
    {
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public string CodeHash { get; set; } = string.Empty;
        public OtpPurpose Purpose { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public int Attempts { get; set; } = 0;
        public int MaxAttempts { get; set; } = 3;
        public string? IpAddress { get; set; }
    }
}

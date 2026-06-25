using System;

namespace Application.Features.Dashboard
{
    public class SettingsDto
    {
        public NotificationsDto Notifications { get; set; } = new();
        public ApiKeyDto Api { get; set; } = new();
        public SubscriptionDetailsDto Subscription { get; set; } = new();
    }

    public class NotificationsDto
    {
        public bool Comments { get; set; }
        public bool Updates { get; set; }
        public bool Marketing { get; set; }
    }

    public class ApiKeyDto
    {
        public bool HasKey { get; set; }
        public DateTime? KeyCreatedAt { get; set; }
    }

    public class SubscriptionDetailsDto
    {
        public string? Plan { get; set; }
        public string Status { get; set; } = "inactive";
        public bool IsActive { get; set; }
        public bool IsTrial { get; set; }
    }
}

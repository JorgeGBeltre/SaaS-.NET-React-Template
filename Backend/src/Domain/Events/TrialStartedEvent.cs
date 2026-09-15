using System;
using Domain.Common;
using Domain.Entities;

namespace Domain.Events
{
    public class TrialStartedEvent : IDomainEvent
    {
        public AppUser User { get; }
        public DateTime TrialEndDate { get; }

        public TrialStartedEvent(AppUser user, DateTime trialEndDate)
        {
            User = user;
            TrialEndDate = trialEndDate;
        }
    }
}

using Domain.Common;
using Domain.Entities;

namespace Domain.Events
{
    public class UserRegisteredEvent : IDomainEvent
    {
        public AppUser User { get; }

        public UserRegisteredEvent(AppUser user)
        {
            User = user;
        }
    }
}

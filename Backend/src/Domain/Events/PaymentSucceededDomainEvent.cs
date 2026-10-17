using System;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Events
{
    public class PaymentSucceededDomainEvent : IDomainEvent
    {
        public PaymentTransaction Transaction { get; }
        public DateTime Timestamp { get; }

        public PaymentSucceededDomainEvent(PaymentTransaction transaction)
        {
            Transaction = transaction;
            Timestamp = DateTime.UtcNow;
        }
    }
}

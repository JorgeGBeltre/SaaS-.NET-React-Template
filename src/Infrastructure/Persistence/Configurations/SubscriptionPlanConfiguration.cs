using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Features)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>(),
                    new ValueComparer<List<string>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()))
                .HasColumnType("text");

            // Seed Subscription Plans
            builder.HasData(
                new SubscriptionPlan
                {
                    // EF core demands key value for seeded data
                    Id = 1,
                    Name = "Basic",
                    Slug = "basic",
                    Description = "Basic plan",
                    Price = 9.99m,
                    Interval = "monthly",
                    Features = new List<string> { "Basic features" },
                    IsActive = true,
                    CreatedAt = new System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc),
                    CreatedBy = "System"
                },
                new SubscriptionPlan
                {
                    Id = 2,
                    Name = "Pro",
                    Slug = "pro",
                    Description = "Pro plan",
                    Price = 29.99m,
                    Interval = "monthly",
                    Features = new List<string> { "Advanced features", "API access" },
                    IsActive = true,
                    CreatedAt = new System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc),
                    CreatedBy = "System"
                }
            );
        }
    }
}

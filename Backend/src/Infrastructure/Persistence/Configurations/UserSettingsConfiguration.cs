using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
    {
        public void Configure(EntityTypeBuilder<UserSettings> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(us => us.User)
                .WithMany()
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(us => us.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(us => us.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}

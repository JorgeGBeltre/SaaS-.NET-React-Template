using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class PaymentCustomerConfiguration : IEntityTypeConfiguration<PaymentCustomer>
    {
        public void Configure(EntityTypeBuilder<PaymentCustomer> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Provider)
                .HasConversion<string>()
                .HasMaxLength(32);

            builder.Property(x => x.ProviderCustomerId)
                .HasMaxLength(128);

            builder.Property(x => x.PaymentProfileId)
                .HasMaxLength(128);

            builder.Property(x => x.PaymentToken)
                .HasMaxLength(256);

            builder.Property(x => x.CardBrand)
                .HasMaxLength(32);

            builder.Property(x => x.CardLast4)
                .HasMaxLength(4);

            builder.Property(x => x.SubscriptionStatus)
                .HasMaxLength(32);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasIndex(x => new { x.UserId, x.Provider });
        }
    }
}

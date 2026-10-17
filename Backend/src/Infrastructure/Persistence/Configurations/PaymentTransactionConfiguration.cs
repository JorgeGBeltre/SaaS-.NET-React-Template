using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Provider)
                .HasConversion<string>()
                .HasMaxLength(32);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32);

            builder.Property(x => x.Currency)
                .HasMaxLength(10);

            builder.Property(x => x.Amount)
                .HasPrecision(18, 2);

            builder.Property(x => x.OrderId)
                .HasMaxLength(128);

            builder.Property(x => x.ProviderTransactionId)
                .HasMaxLength(128);

            builder.Property(x => x.AuthorizationCode)
                .HasMaxLength(64);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.OrderId);
            builder.HasIndex(x => x.ProviderTransactionId);
        }
    }
}

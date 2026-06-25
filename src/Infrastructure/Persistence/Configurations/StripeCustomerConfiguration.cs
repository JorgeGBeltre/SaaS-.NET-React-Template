using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class StripeCustomerConfiguration : IEntityTypeConfiguration<StripeCustomer>
    {
        public void Configure(EntityTypeBuilder<StripeCustomer> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(sc => sc.User)
                .WithMany()
                .HasForeignKey(sc => sc.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        }
    }
}

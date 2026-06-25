using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserProvider _currentUserProvider;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IMediator mediator,
            ICurrentUserProvider currentUserProvider) : base(options)
        {
            _mediator = mediator;
            _currentUserProvider = currentUserProvider;
        }

        public DbSet<AppUser> AppUsers { get; set; } = null!;
        public DbSet<UserSettings> UserSettings { get; set; } = null!;
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public DbSet<StripeCustomer> StripeCustomers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Apply global soft-delete query filter
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .HasQueryFilter(ConvertFilterExpression(entityType.ClrType));
                }
            }

            // Apply snake_case naming conventions
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entity.GetTableName();
                if (tableName != null)
                {
                    entity.SetTableName(ConvertToSnakeCase(tableName));
                }

                foreach (var property in entity.GetProperties())
                {
                    var columnName = property.GetColumnName();
                    if (columnName != null)
                    {
                        property.SetColumnName(ConvertToSnakeCase(columnName));
                    }
                }

                foreach (var key in entity.GetKeys())
                {
                    key.SetName(ConvertToSnakeCase(key.GetName() ?? ""));
                }

                foreach (var fk in entity.GetForeignKeys())
                {
                    fk.SetConstraintName(ConvertToSnakeCase(fk.GetConstraintName() ?? ""));
                }

                foreach (var index in entity.GetIndexes())
                {
                    index.SetDatabaseName(ConvertToSnakeCase(index.GetDatabaseName() ?? ""));
                }
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var currentUser = _currentUserProvider.Email ?? "System";

            // Audit Log implementation
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.CreatedBy = currentUser;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedBy = currentUser;
                        break;

                    case EntityState.Deleted:
                        // Soft delete override
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = DateTime.UtcNow;
                        entry.Entity.DeletedBy = currentUser;
                        break;
                }
            }

            // Capture domain events
            var domainEntities = ChangeTracker.Entries<BaseEntity>()
                .Where(x => x.Entity.DomainEvents.Any())
                .Select(x => x.Entity)
                .ToList();

            var result = await base.SaveChangesAsync(cancellationToken);

            // Dispatch domain events
            foreach (var entity in domainEntities)
            {
                var events = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                foreach (var domainEvent in events)
                {
                    await _mediator.Publish(domainEvent, cancellationToken);
                }
            }

            return result;
        }

        private static LambdaExpression ConvertFilterExpression(Type type)
        {
            var parameter = Expression.Parameter(type, "e");
            var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var notExpression = Expression.Not(property);
            return Expression.Lambda(notExpression, parameter);
        }

        private static string ConvertToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var startUnderscore = input.StartsWith("_");
            var cleanInput = startUnderscore ? input.Substring(1) : input;

            var builder = new StringBuilder();
            for (int i = 0; i < cleanInput.Length; i++)
            {
                char c = cleanInput[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(cleanInput[i - 1]))
                {
                    builder.Append('_');
                }
                builder.Append(char.ToLowerInvariant(c));
            }

            return startUnderscore ? "_" + builder.ToString() : builder.ToString();
        }
    }
}

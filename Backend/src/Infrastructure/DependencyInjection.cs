using Application.Interfaces;
using Domain.Repositories;
using Infrastructure.Authentication;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();

            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Database=saas_db;Username=postgres;Password=supersecretpassword";
            
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            services.AddScoped<IAppUserRepository, AppUserRepository>();
            services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
            services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
            services.AddScoped<IStripeCustomerRepository, StripeCustomerRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IPasswordHasher, PasswordService>();
            services.AddScoped<IJwtProvider, JwtProvider>();
            services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

            return services;
        }
    }
}

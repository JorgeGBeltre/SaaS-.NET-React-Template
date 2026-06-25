using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Application.Features.Auth;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests
{
    public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        private static readonly System.IServiceProvider _sharedInMemoryProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        public EndpointTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var contextDescriptors = services.Where(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                             d.ServiceType == typeof(DbContextOptions) ||
                             d.ServiceType == typeof(ApplicationDbContext)).ToList();

                    foreach (var d in contextDescriptors)
                    {
                        services.Remove(d);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryDbForTesting")
                               .UseInternalServiceProvider(_sharedInMemoryProvider);
                    });
                });
            });
        }

        [Fact]
        public async Task Register_And_Login_Flow_ShouldSucceed()
        {
            var client = _factory.CreateClient();
            var registerDto = new RegisterCommand("integration@example.com", "Password123", "Integration", "Test");

            // Act - Register
            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerDto);
            
            // Assert
            if (registerResponse.StatusCode != HttpStatusCode.OK)
            {
                var content = await registerResponse.Content.ReadAsStringAsync();
                Assert.Fail($"Register failed: {registerResponse.StatusCode}. Body: {content}");
            }

            // Act - Login
            var loginDto = new LoginCommand("integration@example.com", "Password123");
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            if (loginResponse.StatusCode != HttpStatusCode.OK)
            {
                var content = await loginResponse.Content.ReadAsStringAsync();
                Assert.Fail($"Login failed: {loginResponse.StatusCode}. Body: {content}");
            }
            var tokenResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.NotNull(tokenResult);
            Assert.NotEmpty(tokenResult.Token);
        }

        private class LoginResponseDto
        {
            public string Token { get; set; } = string.Empty;
        }
    }
}

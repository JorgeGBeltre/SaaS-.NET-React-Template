using Backend.Controllers;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

namespace Backend.Tests
{
    public class AuthControllerTests
    {
        private ApplicationDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Register_ShouldReturnOk_WhenUserIsNew()
        {
            // Arrange
            var context = GetDbContext("RegisterNewUserDb");
            var controller = new AuthController(context);
            var dto = new RegisterDto
            {
                Email = "newuser@example.com",
                Password = "Password123",
                FirstName = "New",
                LastName = "User"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenUserExists()
        {
            // Arrange
            var context = GetDbContext("RegisterExistingUserDb");
            context.AppUsers.Add(new AppUser { Email = "existing@example.com", PasswordHash = [], PasswordSalt = [] });
            await context.SaveChangesAsync();

            var controller = new AuthController(context);
            var dto = new RegisterDto
            {
                Email = "existing@example.com",
                Password = "Password123"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User already exists", badRequestResult.Value);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
        {
            // Arrange
            var context = GetDbContext("LoginInvalidUserDb");
            var controller = new AuthController(context);
            var dto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "WrongPassword"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid credentials", unauthorizedResult.Value);
        }
    }
}

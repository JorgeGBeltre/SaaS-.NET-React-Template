using System.Threading;
using System.Threading.Tasks;
using Application.Features.Auth;
using Application.Interfaces;
using Domain.Entities;
using Domain.Repositories;
using Moq;
using Shared;
using Xunit;

namespace UnitTests
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<IAppUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IJwtProvider> _jwtProviderMock;
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _userRepositoryMock = new Mock<IAppUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _jwtProviderMock = new Mock<IJwtProvider>();

            _handler = new LoginCommandHandler(
                _userRepositoryMock.Object,
                _passwordHasherMock.Object,
                _jwtProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenCredentialsAreInvalid()
        {
            // Arrange
            var command = new LoginCommand("wrong@example.com", "Password123");
            _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email))
                .ReturnsAsync((AppUser?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Invalid credentials", result.Error);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WithToken_WhenCredentialsAreCorrect()
        {
            // Arrange
            var command = new LoginCommand("correct@example.com", "Password123");
            var user = new AppUser { Email = "correct@example.com", PasswordHash = new byte[] { 1 }, PasswordSalt = new byte[] { 2 } };
            _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.Verify(command.Password, user.PasswordHash, user.PasswordSalt))
                .Returns(true);
            _jwtProviderMock.Setup(x => x.Generate(user))
                .Returns("jwt-token-string");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("jwt-token-string", result.Value);
        }
    }
}

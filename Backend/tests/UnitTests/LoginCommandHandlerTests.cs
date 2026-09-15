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
        private readonly Mock<IUserSessionRepository> _sessionRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _userRepositoryMock = new Mock<IAppUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _jwtProviderMock = new Mock<IJwtProvider>();
            _sessionRepositoryMock = new Mock<IUserSessionRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _handler = new LoginCommandHandler(
                _userRepositoryMock.Object,
                _passwordHasherMock.Object,
                _jwtProviderMock.Object,
                _sessionRepositoryMock.Object,
                _unitOfWorkMock.Object);
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
            var user = new AppUser
            {
                Id = 1,
                Email = "correct@example.com",
                PasswordHash = new byte[] { 1 },
                PasswordSalt = new byte[] { 2 }
            };

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.Verify(command.Password, user.PasswordHash, user.PasswordSalt))
                .Returns(true);
            _jwtProviderMock.Setup(x => x.Generate(user))
                .Returns("jwt-token-string");
            _jwtProviderMock.Setup(x => x.GenerateRefreshToken())
                .Returns("raw-refresh-token");
            _jwtProviderMock.Setup(x => x.HashToken("raw-refresh-token"))
                .Returns("hash-refresh-token");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("jwt-token-string", result.Value.Token);
            Assert.Equal("raw-refresh-token", result.Value.RefreshToken);
            _sessionRepositoryMock.Verify(x => x.Add(It.IsAny<UserSession>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldLockAccount_AfterFiveConsecutiveFailedAttempts()
        {
            // Arrange
            var command = new LoginCommand("victim@example.com", "WrongPassword");
            var user = new AppUser
            {
                Id = 2,
                Email = "victim@example.com",
                FailedLoginAttempts = 4, // 5th failed attempt will lock
                PasswordHash = new byte[] { 1 },
                PasswordSalt = new byte[] { 2 }
            };

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.Verify(command.Password, user.PasswordHash, user.PasswordSalt))
                .Returns(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.True(user.IsLocked);
            Assert.NotNull(user.LockedUntil);
            Assert.Contains("Account locked", result.Error);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldRequire2fa_WhenTotpIsEnabled()
        {
            // Arrange
            var command = new LoginCommand("mfa@example.com", "Password123");
            var user = new AppUser
            {
                Id = 3,
                Email = "mfa@example.com",
                TotpEnabled = true,
                TotpSecret = "JBSWY3DPEHPK3PXP",
                PasswordHash = new byte[] { 1 },
                PasswordSalt = new byte[] { 2 }
            };

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.Verify(command.Password, user.PasswordHash, user.PasswordSalt))
                .Returns(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.Requires2fa);
            Assert.Null(result.Value.Token); // No access token until 2FA code is verified
        }
    }
}

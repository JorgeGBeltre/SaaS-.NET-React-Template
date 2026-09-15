using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Features.Auth;
using Application.Interfaces;
using Domain.Entities;
using Domain.Repositories;
using Moq;
using Xunit;

namespace UnitTests
{
    public class RefreshTokenCommandHandlerTests
    {
        private readonly Mock<IUserSessionRepository> _sessionRepositoryMock;
        private readonly Mock<IAppUserRepository> _userRepositoryMock;
        private readonly Mock<IJwtProvider> _jwtProviderMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly RefreshTokenCommandHandler _handler;

        public RefreshTokenCommandHandlerTests()
        {
            _sessionRepositoryMock = new Mock<IUserSessionRepository>();
            _userRepositoryMock = new Mock<IAppUserRepository>();
            _jwtProviderMock = new Mock<IJwtProvider>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _handler = new RefreshTokenCommandHandler(
                _sessionRepositoryMock.Object,
                _userRepositoryMock.Object,
                _jwtProviderMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldDetectTokenReuse_AndRevokeAllUserSessions_WhenSessionIsInactive()
        {
            // Arrange
            var rawToken = "stolen-or-replayed-token";
            var tokenHash = "hashed-stolen-token";
            var command = new RefreshTokenCommand(null, rawToken);

            var compromisedSession = new UserSession
            {
                Id = 10,
                UserId = 42,
                RefreshTokenHash = tokenHash,
                IsActive = false, // already rotated or revoked!
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            _jwtProviderMock.Setup(x => x.HashToken(rawToken)).Returns(tokenHash);
            _sessionRepositoryMock.Setup(x => x.GetByRefreshTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
                .ReturnsAsync(compromisedSession);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Compromised session detected", result.Error);
            _sessionRepositoryMock.Verify(x => x.RevokeAllUserSessionsAsync(42, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldRotateTokens_AndReturnNewSession_WhenTokenIsValid()
        {
            // Arrange
            var rawToken = "valid-token";
            var tokenHash = "hashed-valid-token";
            var command = new RefreshTokenCommand(null, rawToken);

            var activeSession = new UserSession
            {
                Id = 11,
                UserId = 99,
                RefreshTokenHash = tokenHash,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddDays(5)
            };

            var user = new AppUser
            {
                Id = 99,
                Email = "user99@example.com",
                FirstName = "Alice",
                LastName = "Smith"
            };

            _jwtProviderMock.Setup(x => x.HashToken(rawToken)).Returns(tokenHash);
            _sessionRepositoryMock.Setup(x => x.GetByRefreshTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeSession);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _jwtProviderMock.Setup(x => x.Generate(user)).Returns("new-jwt-access-token");
            _jwtProviderMock.Setup(x => x.GenerateRefreshToken()).Returns("new-raw-refresh-token");
            _jwtProviderMock.Setup(x => x.HashToken("new-raw-refresh-token")).Returns("new-refresh-hash");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("new-jwt-access-token", result.Value.Token);
            Assert.Equal("new-raw-refresh-token", result.Value.RefreshToken);
            Assert.False(activeSession.IsActive); // Old session rotated/deactivated
            _sessionRepositoryMock.Verify(x => x.Add(It.Is<UserSession>(s => s.RefreshTokenHash == "new-refresh-hash" && s.IsActive)), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

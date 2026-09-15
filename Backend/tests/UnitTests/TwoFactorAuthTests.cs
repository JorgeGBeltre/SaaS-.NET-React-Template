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
    public class TwoFactorAuthTests
    {
        private readonly Mock<IAppUserRepository> _userRepositoryMock = new();
        private readonly Mock<ITotpService> _totpServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        [Fact]
        public async Task Enable2fa_ShouldGenerateSecretAndQrUri_WhenUserExists()
        {
            // Arrange
            var user = new AppUser { Id = 7, Email = "2fa@example.com" };
            _userRepositoryMock.Setup(x => x.GetByIdAsync(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _totpServiceMock.Setup(x => x.GenerateSecret(user.Email))
                .Returns(("MYSUPERSECRETKEY", "otpauth://totp/SaaS:2fa@example.com?secret=MYSUPERSECRETKEY"));

            var handler = new Enable2faCommandHandler(
                _userRepositoryMock.Object,
                _totpServiceMock.Object,
                _unitOfWorkMock.Object);

            // Act
            var result = await handler.Handle(new Enable2faCommand(7), CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("MYSUPERSECRETKEY", result.Value.Secret);
            Assert.Equal("MYSUPERSECRETKEY", user.TotpSecret);
            Assert.Contains("otpauth://totp", result.Value.QrCodeUri);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Verify2fa_ShouldEnableTotp_WhenCodeIsValid()
        {
            // Arrange
            var user = new AppUser { Id = 7, Email = "2fa@example.com", TotpSecret = "MYSUPERSECRETKEY", TotpEnabled = false };
            _userRepositoryMock.Setup(x => x.GetByIdAsync(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _totpServiceMock.Setup(x => x.VerifyCode("MYSUPERSECRETKEY", "654321"))
                .Returns(true);

            var handler = new Verify2faCommandHandler(
                _userRepositoryMock.Object,
                _totpServiceMock.Object,
                _unitOfWorkMock.Object);

            // Act
            var result = await handler.Handle(new Verify2faCommand(7, "654321"), CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(user.TotpEnabled);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Disable2fa_ShouldDisableTotp_AndClearSecret_WhenCodeIsValid()
        {
            // Arrange
            var user = new AppUser { Id = 7, Email = "2fa@example.com", TotpSecret = "MYSUPERSECRETKEY", TotpEnabled = true };
            _userRepositoryMock.Setup(x => x.GetByIdAsync(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _totpServiceMock.Setup(x => x.VerifyCode("MYSUPERSECRETKEY", "654321"))
                .Returns(true);

            var handler = new Disable2faCommandHandler(
                _userRepositoryMock.Object,
                _totpServiceMock.Object,
                _unitOfWorkMock.Object);

            // Act
            var result = await handler.Handle(new Disable2faCommand(7, "654321"), CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(user.TotpEnabled);
            Assert.Null(user.TotpSecret);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

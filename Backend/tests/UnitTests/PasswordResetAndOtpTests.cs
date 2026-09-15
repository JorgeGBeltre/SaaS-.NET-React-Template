using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Features.Auth;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Moq;
using Xunit;

namespace UnitTests
{
    public class PasswordResetAndOtpTests
    {
        private readonly Mock<IAppUserRepository> _userRepositoryMock = new();
        private readonly Mock<IUserSessionRepository> _sessionRepositoryMock = new();
        private readonly Mock<IOtpService> _otpServiceMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        [Fact]
        public async Task ForgotPassword_ShouldReturnSuccess_WithoutSendingEmail_WhenUserDoesNotExist()
        {
            // Arrange
            _userRepositoryMock.Setup(x => x.GetByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppUser?)null);

            var handler = new ForgotPasswordCommandHandler(
                _userRepositoryMock.Object,
                _otpServiceMock.Object,
                _emailServiceMock.Object);

            var command = new ForgotPasswordCommand("nobody@example.com");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert (OWASP ASVS anti-enumeration requirement)
            Assert.True(result.IsSuccess);
            _otpServiceMock.Verify(x => x.GenerateOtpAsync(It.IsAny<int>(), It.IsAny<OtpPurpose>(), It.IsAny<string>()), Times.Never);
            _emailServiceMock.Verify(x => x.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OtpPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ForgotPassword_ShouldGenerateOtp_AndSendEmail_WhenUserExists()
        {
            // Arrange
            var user = new AppUser { Id = 5, Email = "realuser@example.com" };
            _userRepositoryMock.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _otpServiceMock.Setup(x => x.GenerateOtpAsync(5, OtpPurpose.PasswordReset, null))
                .ReturnsAsync("123456");

            var handler = new ForgotPasswordCommandHandler(
                _userRepositoryMock.Object,
                _otpServiceMock.Object,
                _emailServiceMock.Object);

            var command = new ForgotPasswordCommand(user.Email);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            _otpServiceMock.Verify(x => x.GenerateOtpAsync(5, OtpPurpose.PasswordReset, null), Times.Once);
            _emailServiceMock.Verify(x => x.SendOtpEmailAsync(user.Email, "123456", OtpPurpose.PasswordReset, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_ShouldFail_WhenOtpIsInvalidOrExpired()
        {
            // Arrange
            var user = new AppUser { Id = 5, Email = "realuser@example.com" };
            _userRepositoryMock.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _otpServiceMock.Setup(x => x.VerifyOtpAsync(5, OtpPurpose.PasswordReset, "999999"))
                .ReturnsAsync(false);

            var handler = new ResetPasswordCommandHandler(
                _userRepositoryMock.Object,
                _sessionRepositoryMock.Object,
                _otpServiceMock.Object,
                _passwordHasherMock.Object,
                _unitOfWorkMock.Object);

            var command = new ResetPasswordCommand(user.Email, "999999", "NewPassword123!");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Invalid or expired reset code.", result.Error);
            _sessionRepositoryMock.Verify(x => x.RevokeAllUserSessionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ResetPassword_ShouldUpdatePassword_AndRevokeAllSessions_WhenOtpIsValid()
        {
            // Arrange
            var user = new AppUser { Id = 5, Email = "realuser@example.com", IsLocked = true };
            _userRepositoryMock.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _otpServiceMock.Setup(x => x.VerifyOtpAsync(5, OtpPurpose.PasswordReset, "123456"))
                .ReturnsAsync(true);
            _passwordHasherMock.Setup(x => x.CreateHash("NewPassword123!"))
                .Returns((new byte[] { 10 }, new byte[] { 20 }));

            var handler = new ResetPasswordCommandHandler(
                _userRepositoryMock.Object,
                _sessionRepositoryMock.Object,
                _otpServiceMock.Object,
                _passwordHasherMock.Object,
                _unitOfWorkMock.Object);

            var command = new ResetPasswordCommand(user.Email, "123456", "NewPassword123!");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(user.IsLocked);
            Assert.Equal(new byte[] { 10 }, user.PasswordHash);
            Assert.Equal(new byte[] { 20 }, user.PasswordSalt);
            _sessionRepositoryMock.Verify(x => x.RevokeAllUserSessionsAsync(5, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

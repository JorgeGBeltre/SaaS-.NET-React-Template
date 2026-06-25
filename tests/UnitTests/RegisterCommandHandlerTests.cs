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
    public class RegisterCommandHandlerTests
    {
        private readonly Mock<IAppUserRepository> _userRepositoryMock;
        private readonly Mock<IUserSettingsRepository> _settingsRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly RegisterCommandHandler _handler;

        public RegisterCommandHandlerTests()
        {
            _userRepositoryMock = new Mock<IAppUserRepository>();
            _settingsRepositoryMock = new Mock<IUserSettingsRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _handler = new RegisterCommandHandler(
                _userRepositoryMock.Object,
                _settingsRepositoryMock.Object,
                _passwordHasherMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenUserAlreadyExists()
        {
            // Arrange
            var command = new RegisterCommand("existing@example.com", "Password123", "First", "Last");
            _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(command.Email))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("User already exists", result.Error);
            _userRepositoryMock.Verify(x => x.Add(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenUserIsNew()
        {
            // Arrange
            var command = new RegisterCommand("new@example.com", "Password123", "First", "Last");
            _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(command.Email))
                .ReturnsAsync(false);
            _passwordHasherMock.Setup(x => x.CreateHash(command.Password))
                .Returns((new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            _userRepositoryMock.Verify(x => x.Add(It.Is<AppUser>(u => u.Email == command.Email && u.FirstName == command.FirstName)), Times.Once);
            _settingsRepositoryMock.Verify(x => x.Add(It.IsAny<UserSettings>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}

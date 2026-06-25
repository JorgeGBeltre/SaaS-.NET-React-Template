using Application.Features.Auth;
using Xunit;

namespace UnitTests
{
    public class RegisterValidatorTests
    {
        private readonly RegisterValidator _validator = new();

        [Fact]
        public void Validate_ShouldHaveErrors_WhenFieldsAreEmpty()
        {
            var command = new RegisterCommand("", "", "", "");
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, x => x.PropertyName == nameof(RegisterCommand.Email));
            Assert.Contains(result.Errors, x => x.PropertyName == nameof(RegisterCommand.Password));
            Assert.Contains(result.Errors, x => x.PropertyName == nameof(RegisterCommand.FirstName));
            Assert.Contains(result.Errors, x => x.PropertyName == nameof(RegisterCommand.LastName));
        }

        [Fact]
        public void Validate_ShouldBeValid_WhenAllFieldsAreValid()
        {
            var command = new RegisterCommand("test@example.com", "Password123", "First", "Last");
            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }
    }
}

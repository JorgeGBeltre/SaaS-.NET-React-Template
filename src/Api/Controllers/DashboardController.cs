using System.Security.Claims;
using System.Threading.Tasks;
using Application.Features.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    public class DashboardController : ApiControllerBase
    {
        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var result = await Mediator.Send(new GetProfileQuery(email));
            return MatchResult(result);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var command = new UpdateProfileCommand(email, request.FirstName, request.LastName);
            var result = await Mediator.Send(command);
            return MatchResult(result, new { message = "Profile updated" });
        }

        [HttpGet("settings")]
        public async Task<IActionResult> Settings()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var result = await Mediator.Send(new GetSettingsQuery(email));
            if (result.IsFailure)
            {
                return NotFound(new { hasKey = false });
            }
            return Ok(result.Value);
        }

        [HttpPost("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var command = new UpdateSettingsCommand(email, request.NotifyComments, request.NotifyUpdates, request.NotifyMarketing);
            var result = await Mediator.Send(command);
            return MatchResult(result, new { message = "Settings updated" });
        }

        [HttpPost("generate-api-key")]
        public async Task<IActionResult> GenerateApiKey()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var result = await Mediator.Send(new GenerateApiKeyCommand(email));
            return MatchResult(result, apiKey => new { message = "API key generated", apiKey });
        }

        [HttpGet("subscription-plans")]
        public async Task<IActionResult> SubscriptionPlans()
        {
            var result = await Mediator.Send(new GetSubscriptionPlansQuery());
            return MatchResult(result);
        }

        [HttpPost("start-trial")]
        public async Task<IActionResult> StartTrial()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var result = await Mediator.Send(new StartTrialCommand(email));
            return MatchResult(result, new { message = "Trial started" });
        }
    }

    public record UpdateProfileRequest(string FirstName, string LastName);
    public record UpdateSettingsRequest(bool NotifyComments, bool NotifyUpdates, bool NotifyMarketing);
}

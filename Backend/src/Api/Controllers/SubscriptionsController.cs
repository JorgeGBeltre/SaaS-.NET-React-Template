using System.Security.Claims;
using System.Threading.Tasks;
using Application.Features.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class SubscriptionsController : ApiControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Subscription()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var result = await Mediator.Send(new GetSubscriptionQuery(email));
            return MatchResult(result);
        }
    }
}

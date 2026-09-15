using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class ApiControllerBase : ControllerBase
    {
        private ISender? _mediator;

        protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

        protected IActionResult MatchResult(Result result, object? successValue = null)
        {
            if (result.IsSuccess)
            {
                return successValue != null ? Ok(successValue) : Ok();
            }

            if (result.Error == "Invalid credentials" || result.Error == "Unauthorized")
            {
                return Unauthorized(result.Error);
            }

            if (result.Error.Contains("not found") || result.Error.Contains("NotFound"))
            {
                return NotFound(result.Error);
            }

            return BadRequest(result.Error);
        }

        protected IActionResult MatchResult<T>(Result<T> result, Func<T, object>? mapper = null)
        {
            if (result.IsSuccess)
            {
                return mapper != null ? Ok(mapper(result.Value)) : Ok(result.Value);
            }

            if (result.Error == "Invalid credentials" || result.Error == "Unauthorized")
            {
                return Unauthorized(result.Error);
            }

            if (result.Error.Contains("not found") || result.Error.Contains("NotFound"))
            {
                return NotFound(result.Error);
            }

            return BadRequest(result.Error);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Subscription()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var stripeCustomer = await _context.StripeCustomers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.Email == email);
            if (stripeCustomer == null) return Ok(new { hasSubscription = false });

            return Ok(new { 
                stripeCustomerId = stripeCustomer.StripeCustomerId,
                subscriptionId = stripeCustomer.StripeSubscriptionId,
                status = stripeCustomer.SubscriptionStatus
            });
        }
    }
}


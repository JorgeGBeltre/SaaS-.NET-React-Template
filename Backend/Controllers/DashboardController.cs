using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound();

            return Ok(new { email = user.Email, firstName = user.FirstName, lastName = user.LastName });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound();

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated" });
        }

        [HttpGet("settings")]
        public async Task<IActionResult> Settings()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var settings = await _context.UserSettings.Include(s => s.SubscriptionPlan).FirstOrDefaultAsync(s => s.User.Email == email);
            if (settings == null) return NotFound(new { hasKey = false });

            var now = DateTime.UtcNow;
            var isSubscriptionActive = settings.SubscriptionStatus == "active" && 
                (!settings.SubscriptionEndDate.HasValue || settings.SubscriptionEndDate > now);
            var isTrialActive = settings.SubscriptionStatus == "trial" && 
                (!settings.TrialEndDate.HasValue || settings.TrialEndDate > now);

            return Ok(new { 
                notifications = new {
                    comments = settings.NotifyComments,
                    updates = settings.NotifyUpdates,
                    marketing = settings.NotifyMarketing
                },
                api = new {
                    hasKey = !string.IsNullOrEmpty(settings.ApiKey),
                    keyCreatedAt = settings.ApiKeyCreatedAt
                },
                subscription = new {
                    plan = settings.SubscriptionPlan?.Name,
                    status = settings.SubscriptionStatus,
                    isActive = isSubscriptionActive,
                    isTrial = isTrialActive
                }
            });
        }

        [HttpPost("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsDto dto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.User.Email == email);
            if (settings == null) return NotFound();

            settings.NotifyComments = dto.NotifyComments;
            settings.NotifyUpdates = dto.NotifyUpdates;
            settings.NotifyMarketing = dto.NotifyMarketing;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Settings updated" });
        }

        [HttpPost("generate-api-key")]
        public async Task<IActionResult> GenerateApiKey()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.User.Email == email);
            if (settings == null) return NotFound();

            settings.ApiKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            settings.ApiKeyCreatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "API key generated", apiKey = settings.ApiKey });
        }

        [HttpGet("subscription-plans")]
        public async Task<IActionResult> SubscriptionPlans()
        {
            var plans = await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync();
            return Ok(plans.Select(p => new { 
                id = p.Id,
                name = p.Name,
                slug = p.Slug,
                price = p.Price,
                interval = p.Interval,
                features = p.Features
            }));
        }

        [HttpPost("start-trial")]
        public async Task<IActionResult> StartTrial()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.User.Email == email);
            if (settings == null) return NotFound();

            var now = DateTime.UtcNow;
            var isSubscriptionActive = settings.SubscriptionStatus == "active" && 
                (!settings.SubscriptionEndDate.HasValue || settings.SubscriptionEndDate > now);
            var isTrialActive = settings.SubscriptionStatus == "trial" && 
                (!settings.TrialEndDate.HasValue || settings.TrialEndDate > now);

            if (isSubscriptionActive || isTrialActive)
                return BadRequest("Already subscribed or in trial");

            settings.SubscriptionStatus = "trial";
            settings.TrialEndDate = DateTime.UtcNow.AddDays(14);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Trial started" });
        }
    }

    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    public class UpdateSettingsDto
    {
        public bool NotifyComments { get; set; }
        public bool NotifyUpdates { get; set; }
        public bool NotifyMarketing { get; set; }
    }
}

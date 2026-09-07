using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;
using SicBoLive.Infrastructure.Identity;

namespace SicBoLive.WebApi.Controllers;

public record ProvisionUserRequest(string UserName, string Password, string DisplayName, int PlanMonths);
public record ProvisionUserResult(Guid Id, string UserName, string DisplayName);

/// <summary>
/// Machine-to-machine account provisioning for the shared Telegram bot (@game2652bot, display
/// name "game2026bot" - see D:\SikboLive\telegram-bot\bot.js). Mirrors PokerLive's
/// InternalController.cs pattern exactly (X-Internal-Key header check against InternalApi:Key
/// config), so the bot's existing provisionGameUser() helper can call this endpoint the same way
/// it already calls Poker's.
///
/// This project's own Services/TelegramBotService.cs stays disabled in production
/// (Telegram:BotToken left blank there, same as Poker) - the one shared bot handles registration
/// for every game and calls this endpoint after its own admin-approval flow.
///
/// IMPORTANT (bug found and fixed here, not yet fixed in Poker's copy as of this writing):
/// bot.js hands the customer the raw generated username (e.g. "sicbo_bpEGa") as their literal
/// login credential - it never appends any "@domain" suffix in the message it sends. Since
/// AuthController.Login looks the user up by Email (FindByEmailAsync), Email must be set to
/// EXACTLY request.UserName here, with no suffix appended - otherwise the customer's login
/// attempt with the credential the bot showed them would always fail. (Identity's
/// RequireUniqueEmail defaults to false and Email format is never validated for these bot-issued
/// accounts anyway, same convention already used by TelegramBotService's own
/// GenerateUniqueUsernameAsync.)
///
/// PlanMonths (1/2/3) selects the Nth active plan ordered by ascending duration - same ordinal
/// convention TelegramBotService's plan-picker keyboard uses, so both provisioning paths agree on
/// what "plan 1/2/3" means.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/internal")]
public class InternalController(UserManager<ApplicationUser> userManager, IApplicationDbContext db, IConfiguration config)
    : ControllerBase
{
    [HttpPost("provision-user")]
    public async Task<IActionResult> ProvisionUser(ProvisionUserRequest request, CancellationToken ct)
    {
        var expectedKey = config["InternalApi:Key"];
        if (string.IsNullOrEmpty(expectedKey) || Request.Headers["X-Internal-Key"] != expectedKey)
            return Unauthorized(new ProblemDetails { Status = 401, Detail = "کلید داخلی نامعتبر است." });

        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest(new ProblemDetails { Status = 400, Detail = "اطلاعات ورودی ناقص است." });

        if (await userManager.FindByNameAsync(request.UserName) is not null || await userManager.FindByEmailAsync(request.UserName) is not null)
            return Conflict(new ProblemDetails { Status = 409, Detail = "این نام کاربری قبلاً استفاده شده است." });

        var plan = await GetPlanByOrdinalAsync(request.PlanMonths, ct);
        if (plan is null)
            return BadRequest(new ProblemDetails { Status = 400, Detail = "طرح اشتراک معتبری برای این مدت یافت نشد." });

        var adminId = await userManager.Users.Where(u => u.Role == UserRole.Admin).Select(u => u.Id).FirstOrDefaultAsync(ct);
        if (adminId == Guid.Empty)
            return StatusCode(500, new ProblemDetails { Status = 500, Detail = "حساب مدیر سیستم یافت نشد." });

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            // No suffix - see the class-level doc comment. The customer logs in with exactly what
            // the bot showed them, and the bot shows them the raw UserName.
            Email = request.UserName,
            DisplayName = request.DisplayName,
            Role = UserRole.Player,
            TelegramChatId = null,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(new ProblemDetails { Status = 400, Detail = string.Join("; ", createResult.Errors.Select(e => e.Description)) });

        db.PlayerSubscriptions.Add(new PlayerSubscription(user.Id, plan.Id, adminId, plan.GameDurationMinutes));
        await db.SaveChangesAsync(ct);

        return Ok(new ProvisionUserResult(user.Id, user.UserName!, user.DisplayName));
    }

    private async Task<SubscriptionPlan?> GetPlanByOrdinalAsync(int planIndex, CancellationToken ct)
    {
        if (planIndex < 1)
            return null;

        var plans = await db.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.GameDurationMinutes)
            .ToListAsync(ct);

        return planIndex <= plans.Count ? plans[planIndex - 1] : null;
    }
}

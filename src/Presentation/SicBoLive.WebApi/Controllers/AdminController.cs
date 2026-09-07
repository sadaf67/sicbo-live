using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SicBoLive.Application.Admin.Commands.ActivateSubscription;
using SicBoLive.Application.Admin.Commands.CreateSubscriptionPlan;
using SicBoLive.Application.Admin.Commands.DeactivateSubscriptionPlan;
using SicBoLive.Application.Admin.Commands.RevokeSubscription;
using SicBoLive.Application.Admin.Commands.UpdateSubscriptionPlan;
using SicBoLive.Application.Admin.Queries.GetAllGroups;
using SicBoLive.Application.Admin.Queries.GetAllPlayers;
using SicBoLive.Application.Admin.Queries.GetPlayerVerificationPhoto;
using SicBoLive.Application.Admin.Queries.GetSubscriptionPlans;
using SicBoLive.Domain.Enums;
using SicBoLive.Infrastructure.Identity;
using SicBoLive.WebApi.Services;

namespace SicBoLive.WebApi.Controllers;

public record CreatePlayerBody(string DisplayName);
public record CreatePlayerResult(Guid UserId, string Username, string Password, string DisplayName);

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController(IMediator mediator, UserManager<ApplicationUser> userManager) : ControllerBase
{
    // Manual account creation for when the admin wants to hand someone a login without going
    // through the Telegram bot at all (e.g. in person, or for herself while testing). Mints
    // credentials the exact same way the bot does (same charset/uniqueness convention, see
    // CredentialGenerator) and creates a plain Player account with no subscription - the admin
    // activates a subscription for them afterward from the players list below, same as any other
    // account. The password is only ever returned in this one response; it is not recoverable
    // later (the admin would need to tell the player to use the Telegram bot's /resetpassword flow
    // - but that only works for bot-linked accounts, so admin-created accounts with a forgotten
    // password currently have no self-service reset; not a concern for the friends-and-family
    // scale this is built for today).
    [HttpPost("players")]
    public async Task<ActionResult<CreatePlayerResult>> CreatePlayer(CreatePlayerBody body)
    {
        var displayName = body.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
            return BadRequest("نام نمایشی الزامی است.");

        var loginName = await CredentialGenerator.GenerateUniqueUsernameAsync(userManager, displayName, "SicBo", HttpContext.RequestAborted);
        var systemUserName = await CredentialGenerator.GenerateUniqueSystemUserNameAsync(userManager, "admin", HttpContext.RequestAborted);
        var password = CredentialGenerator.GenerateRandomPassword();

        var user = new ApplicationUser
        {
            // UserName can't hold the freeform "{Name}@{GameCode}{N}" string (Identity rejects "@"
            // and Persian letters there by default) - see CredentialGenerator.
            // GenerateUniqueSystemUserNameAsync's doc comment. Login only ever checks Email.
            UserName = systemUserName,
            Email = loginName,
            DisplayName = displayName,
            Role = UserRole.Player,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return Ok(new CreatePlayerResult(user.Id, loginName, password, displayName));
    }

    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanDto>>> GetPlans() =>
        Ok(await mediator.Send(new GetSubscriptionPlansQuery()));

    [HttpPost("plans")]
    public async Task<ActionResult<Guid>> CreatePlan(CreateSubscriptionPlanCommand command) =>
        Ok(await mediator.Send(command));

    [HttpPut("plans/{planId:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid planId, UpdatePlanBody body)
    {
        await mediator.Send(new UpdateSubscriptionPlanCommand(planId, body.EntryTokenAmount, body.GameDurationMinutes, body.DiscountPercent));
        return NoContent();
    }

    [HttpDelete("plans/{planId:guid}")]
    public async Task<IActionResult> DeactivatePlan(Guid planId)
    {
        await mediator.Send(new DeactivateSubscriptionPlanCommand(planId));
        return NoContent();
    }

    [HttpGet("groups")]
    public async Task<ActionResult<IReadOnlyList<AdminGroupDto>>> GetGroups() =>
        Ok(await mediator.Send(new GetAllGroupsQuery()));

    [HttpGet("players")]
    public async Task<ActionResult<IReadOnlyList<PlayerAdminDto>>> GetPlayers() =>
        Ok(await mediator.Send(new GetAllPlayersQuery()));

    [HttpPost("players/{userId:guid}/subscription")]
    public async Task<ActionResult<Guid>> ActivateSubscription(Guid userId, ActivateSubscriptionBody body) =>
        Ok(await mediator.Send(new ActivateSubscriptionCommand(userId, body.PlanId)));

    [HttpDelete("players/{userId:guid}/subscription")]
    public async Task<IActionResult> RevokeSubscription(Guid userId)
    {
        await mediator.Send(new RevokeSubscriptionCommand(userId));
        return NoContent();
    }

    // Identity-verification selfie captured at registration (see ApplicationUser.VerificationPhoto).
    // Returned as a raw image so the admin UI can drop it straight into an <img src> via a blob URL;
    // 404 (not an empty 200) when the account predates this feature and has no photo on file.
    [HttpGet("players/{userId:guid}/photo")]
    public async Task<IActionResult> GetVerificationPhoto(Guid userId)
    {
        var photo = await mediator.Send(new GetPlayerVerificationPhotoQuery(userId));
        if (photo is null)
            return NotFound();

        return File(photo, "image/jpeg");
    }
}

public record ActivateSubscriptionBody(Guid PlanId);

public record UpdatePlanBody(decimal EntryTokenAmount, int GameDurationMinutes, decimal DiscountPercent);

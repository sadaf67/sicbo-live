using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SicBoLive.Domain.Enums;
using SicBoLive.Infrastructure.Identity;
using SicBoLive.WebApi.Services;

namespace SicBoLive.WebApi.Controllers;

// There is no public self-registration endpoint anymore: accounts are created by the Telegram bot's
// /start flow (see TelegramBotService), which mints a unique username/password pair and shows it to
// the player once. This controller only ever authenticates an existing account.
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, Guid UserId, string DisplayName, UserRole Role);

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, JwtTokenService jwtTokenService)
    : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized("نام کاربری یا رمز عبور نادرست است.");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized("نام کاربری یا رمز عبور نادرست است.");

        // Anti-account-sharing control: mint a fresh session id on every successful login and
        // persist it *before* baking it into the token. Program.cs's OnTokenValidated checks every
        // request's "sid" claim against this DB value, so this line is what makes the previous
        // device's still-unexpired token stop working the instant someone logs in elsewhere with
        // the same email/password - i.e. only one device can have a live session per account.
        user.CurrentSessionId = Guid.NewGuid();
        await userManager.UpdateAsync(user);

        var token = jwtTokenService.CreateToken(user);
        return Ok(new AuthResponse(token, user.Id, user.DisplayName, user.Role));
    }

    // Lets a dealer become a player (or vice-versa) at any time, per the product decision that
    // roles are just a UI/permissions mode - not a fixed identity - for this points-only game. This
    // is also how a bot-created account (always minted as Player - see TelegramBotService) becomes a
    // dealer: there's no separate "register as dealer" path anymore, the player just switches once
    // logged in. Admin is excluded. Because every server-side role check (ICurrentUserService.Role)
    // reads the role out of the JWT claim - never the database - we must mint and hand back a
    // brand-new token here, or the switch would look like it worked client-side while every
    // role-gated call kept using the old role.
    [Authorize]
    [HttpPost("switch-role")]
    public async Task<ActionResult<AuthResponse>> SwitchRole()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        if (user.Role == UserRole.Admin)
            return BadRequest(new[] { "مدیر نمی‌تواند نقش خود را تغییر دهد." });

        user.Role = user.Role == UserRole.Dealer ? UserRole.Player : UserRole.Dealer;
        // Also rotates the session id here (same reasoning as Login above) - not strictly required
        // for the anti-sharing goal on its own, but keeps the invariant simple: every freshly-minted
        // token, from any of these three endpoints, always carries the *current* sid.
        user.CurrentSessionId = Guid.NewGuid();
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var token = jwtTokenService.CreateToken(user);
        return Ok(new AuthResponse(token, user.Id, user.DisplayName, user.Role));
    }
}

using Microsoft.AspNetCore.Identity;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = default!;
    public UserRole Role { get; set; } = UserRole.Player;

    /// <summary>
    /// Identifies the single currently-valid JWT for this account. Refreshed to a new value on every
    /// Register/Login/SwitchRole (see JwtTokenService + AuthController) and embedded in the JWT as the
    /// "sid" claim. The JwtBearer OnTokenValidated event (Program.cs) rejects any token whose "sid"
    /// doesn't match this value - so logging in from a second device instantly invalidates the first
    /// device's token, enforcing "one active session per account" without needing a payment gateway or
    /// any DRM: this is the anti-account-sharing control (see CLAUDE.md "Single active session" section).
    /// </summary>
    public Guid CurrentSessionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Optional identity-verification selfie captured client-side (webcam getUserMedia + canvas
    /// snapshot), stored as raw JPEG bytes. Purpose: let an admin visually confirm, on demand, that
    /// the person who registered the account is the same person shown live in the LiveKit video panel
    /// during play - this is the "photo/selfie" layer of the anti-account-sharing plan (see CLAUDE.md
    /// "Single active session" section for the other two layers: the session-lock and the admin revoke
    /// button). Legacy field: web self-registration (which used to collect this) has been replaced by
    /// Telegram-bot auto-registration (see TelegramBotService), which does not collect a selfie at all.
    /// So this is only ever populated for accounts created before that change; always null for newer,
    /// bot-created accounts.
    /// </summary>
    public byte[]? VerificationPhoto { get; set; }

    /// <summary>
    /// Telegram chat id of the account holder, set when the account was created (or later linked) via
    /// the Telegram bot's auto-registration flow (see TelegramBotService). Lets /start recognize a
    /// returning Telegram user and reuse their existing account instead of minting a duplicate one, and
    /// lets /resetpassword know which account to issue a fresh random password for. Null for accounts
    /// created before this feature shipped (e.g. the seeded admin account).
    /// </summary>
    public long? TelegramChatId { get; set; }
}

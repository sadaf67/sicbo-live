using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Infrastructure.Identity;

namespace SicBoLive.WebApi.Services;

/// <summary>
/// Shared username/password generation, extracted out of TelegramBotService so AdminController's
/// manual "create player account" endpoint (for when the admin wants to hand out a login without
/// going through the Telegram bot at all) can mint credentials the exact same way the bot does -
/// same unambiguous charset, same uniqueness-walk convention.
/// </summary>
public static class CredentialGenerator
{
    // Visually-unambiguous charset for random passwords/usernames handed out over a chat window or
    // read aloud (excludes 0/O and 1/l/I, which are easy to mistype when copying by eye).
    private const string PasswordChars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789";

    // Builds "{Name}@{GameCode}{N}" and, since Identity's Email field is NOT uniqueness-checked
    // (RequireUniqueEmail defaults to false), manually walks N upward starting from the current
    // total user count until it lands on a value nobody has yet. This is the login-facing
    // identifier (AuthController.Login looks users up by Email, not UserName).
    public static async Task<string> GenerateUniqueUsernameAsync(UserManager<ApplicationUser> userManager, string displayName, string gameCode, CancellationToken cancellationToken)
    {
        // "@" would collide with the Name@GameCode separator, and email fields elsewhere in the UI
        // don't expect embedded newlines/whitespace - the rest of the string (including Persian
        // letters) is left as-is since Email format is never validated for these accounts.
        var sanitizedName = displayName.Replace("@", "", StringComparison.Ordinal).Trim();
        if (sanitizedName.Length == 0)
            sanitizedName = "کاربر";

        var candidateNumber = await userManager.Users.CountAsync(cancellationToken) + 1000;
        string candidate;
        do
        {
            candidate = $"{sanitizedName}@{gameCode}{candidateNumber}";
            candidateNumber++;
        }
        while (await userManager.Users.AnyAsync(u => u.Email == candidate, cancellationToken));

        return candidate;
    }

    // Identity's UserName field (unlike Email) is restricted by AllowedUserNameCharacters (default:
    // alphanumerics + a few ASCII symbols - no "@", no Persian letters), so it can't hold the same
    // freeform "{Name}@{GameCode}{N}" string GenerateUniqueUsernameAsync builds for Email. Every
    // account still needs *some* UserName value even though login only ever checks Email
    // (AuthController.Login uses FindByEmailAsync) - TelegramBotService satisfies this with the
    // Telegram chat id ("tg{chatId}"); this is the equivalent for admin-created accounts, which have
    // no chat id to key off of.
    public static async Task<string> GenerateUniqueSystemUserNameAsync(UserManager<ApplicationUser> userManager, string prefix, CancellationToken cancellationToken)
    {
        string candidate;
        do
        {
            candidate = $"{prefix}{GenerateRandomPassword()}";
        }
        while (await userManager.Users.AnyAsync(u => u.UserName == candidate, cancellationToken));

        return candidate;
    }

    // ~10 unambiguous characters, with at least one lowercase letter and one digit forced in so the
    // result always satisfies the configured Identity password policy (RequiredLength=6,
    // RequireLowercase=true, RequireDigit=true, RequireNonAlphanumeric=false, RequireUppercase=false -
    // see DependencyInjection.cs) regardless of what RandomNumberGenerator happens to pick.
    public static string GenerateRandomPassword()
    {
        const int length = 10;
        Span<char> buffer = stackalloc char[length];
        for (var i = 0; i < length; i++)
            buffer[i] = PasswordChars[System.Security.Cryptography.RandomNumberGenerator.GetInt32(PasswordChars.Length)];

        // Guarantee at least one lowercase letter and one digit by overwriting two fixed slots -
        // simpler and just as random-looking as a rejection-sampling loop, and the first two
        // characters of a 10-char random string carry no meaningful positional pattern.
        const string lowercase = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        buffer[0] = lowercase[System.Security.Cryptography.RandomNumberGenerator.GetInt32(lowercase.Length)];
        buffer[1] = digits[System.Security.Cryptography.RandomNumberGenerator.GetInt32(digits.Length)];

        return new string(buffer);
    }
}

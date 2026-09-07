using Microsoft.AspNetCore.Identity;

namespace SicBoLive.Infrastructure.Identity;

/// <summary>Translates ASP.NET Identity's built-in validation error messages into Persian.</summary>
public class PersianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new()
    {
        Code = nameof(DefaultError),
        Description = "خطای ناشناخته رخ داد.",
    };

    public override IdentityError ConcurrencyFailure() => new()
    {
        Code = nameof(ConcurrencyFailure),
        Description = "تغییرات همزمان با اطلاعات فعلی سازگار نیست.",
    };

    public override IdentityError PasswordMismatch() => new()
    {
        Code = nameof(PasswordMismatch),
        Description = "رمز عبور نادرست است.",
    };

    public override IdentityError InvalidToken() => new()
    {
        Code = nameof(InvalidToken),
        Description = "توکن نامعتبر است.",
    };

    public override IdentityError LoginAlreadyAssociated() => new()
    {
        Code = nameof(LoginAlreadyAssociated),
        Description = "کاربری با این اطلاعات ورود از قبل وجود دارد.",
    };

    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = nameof(InvalidUserName),
        Description = $"نام کاربری «{userName}» نامعتبر است.",
    };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = nameof(InvalidEmail),
        Description = $"ایمیل «{email}» نامعتبر است.",
    };

    public override IdentityError DuplicateUserName(string userName) => new()
    {
        Code = nameof(DuplicateUserName),
        Description = $"نام کاربری «{userName}» قبلاً استفاده شده است.",
    };

    public override IdentityError DuplicateEmail(string email) => new()
    {
        Code = nameof(DuplicateEmail),
        Description = $"این ایمیل قبلاً ثبت شده است.",
    };

    public override IdentityError InvalidRoleName(string? role) => new()
    {
        Code = nameof(InvalidRoleName),
        Description = $"نام نقش «{role}» نامعتبر است.",
    };

    public override IdentityError DuplicateRoleName(string role) => new()
    {
        Code = nameof(DuplicateRoleName),
        Description = $"نقش «{role}» قبلاً وجود دارد.",
    };

    public override IdentityError UserAlreadyHasPassword() => new()
    {
        Code = nameof(UserAlreadyHasPassword),
        Description = "این کاربر از قبل رمز عبور دارد.",
    };

    public override IdentityError UserLockoutNotEnabled() => new()
    {
        Code = nameof(UserLockoutNotEnabled),
        Description = "قفل‌شدن حساب برای این کاربر فعال نیست.",
    };

    public override IdentityError UserAlreadyInRole(string role) => new()
    {
        Code = nameof(UserAlreadyInRole),
        Description = $"کاربر از قبل نقش «{role}» را دارد.",
    };

    public override IdentityError UserNotInRole(string role) => new()
    {
        Code = nameof(UserNotInRole),
        Description = $"کاربر نقش «{role}» را ندارد.",
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = $"رمز عبور باید حداقل {length} کاراکتر باشد.",
    };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
    {
        Code = nameof(PasswordRequiresUniqueChars),
        Description = $"رمز عبور باید حداقل شامل {uniqueChars} کاراکتر متمایز باشد.",
    };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new()
    {
        Code = nameof(PasswordRequiresNonAlphanumeric),
        Description = "رمز عبور باید حداقل شامل یک کاراکتر غیر الفبایی‌عددی باشد.",
    };

    public override IdentityError PasswordRequiresDigit() => new()
    {
        Code = nameof(PasswordRequiresDigit),
        Description = "رمز عبور باید حداقل شامل یک رقم ('0'-'9') باشد.",
    };

    public override IdentityError PasswordRequiresLower() => new()
    {
        Code = nameof(PasswordRequiresLower),
        Description = "رمز عبور باید حداقل شامل یک حرف کوچک لاتین ('a'-'z') باشد.",
    };

    public override IdentityError PasswordRequiresUpper() => new()
    {
        Code = nameof(PasswordRequiresUpper),
        Description = "رمز عبور باید حداقل شامل یک حرف بزرگ لاتین ('A'-'Z') باشد.",
    };

    public override IdentityError RecoveryCodeRedemptionFailed() => new()
    {
        Code = nameof(RecoveryCodeRedemptionFailed),
        Description = "استفاده از کد بازیابی ناموفق بود.",
    };
}

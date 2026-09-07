using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException("No authenticated user.");
        }
    }

    public UserRole Role
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(value, out var role) ? role : throw new UnauthorizedAccessException("No authenticated user role.");
        }
    }
}

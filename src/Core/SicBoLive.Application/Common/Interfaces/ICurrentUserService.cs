using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    UserRole Role { get; }
}

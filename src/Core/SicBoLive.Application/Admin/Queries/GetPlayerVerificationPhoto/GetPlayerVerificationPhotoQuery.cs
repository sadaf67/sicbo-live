using MediatR;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Admin.Queries.GetPlayerVerificationPhoto;

/// <summary>Admin-only lookup of a player's mandatory registration-time selfie (see
/// ApplicationUser.VerificationPhoto). Returns null if the player has none on file (accounts created
/// before this feature shipped) rather than throwing, so the controller can 404 cleanly.</summary>
public record GetPlayerVerificationPhotoQuery(Guid UserId) : IRequest<byte[]?>;

public class GetPlayerVerificationPhotoQueryHandler(IUserDirectory userDirectory, ICurrentUserService currentUser)
    : IRequestHandler<GetPlayerVerificationPhotoQuery, byte[]?>
{
    public Task<byte[]?> Handle(GetPlayerVerificationPhotoQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("فقط ادمین می‌تواند عکس احراز هویت را ببیند.");

        return userDirectory.GetVerificationPhotoAsync(request.UserId, cancellationToken);
    }
}

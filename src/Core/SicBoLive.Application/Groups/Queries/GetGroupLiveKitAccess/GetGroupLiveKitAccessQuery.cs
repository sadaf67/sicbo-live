using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;

namespace SicBoLive.Application.Groups.Queries.GetGroupLiveKitAccess;

public record LiveKitAccessDto(string RoomName, string Identity, string ParticipantName);

public record GetGroupLiveKitAccessQuery(Guid GroupId) : IRequest<LiveKitAccessDto>;

public class GetGroupLiveKitAccessQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser, IUserDirectory userDirectory)
    : IRequestHandler<GetGroupLiveKitAccessQuery, LiveKitAccessDto>
{
    public async Task<LiveKitAccessDto> Handle(GetGroupLiveKitAccessQuery request, CancellationToken cancellationToken)
    {
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        var isDealer = group.DealerId == currentUser.UserId;
        if (!isDealer)
        {
            var isMember = await db.Wallets.AnyAsync(w => w.GroupId == request.GroupId && w.UserId == currentUser.UserId, cancellationToken);
            if (!isMember)
                throw new UnauthorizedAccessException("شما عضو این میز نیستید.");
        }

        var names = await userDirectory.GetDisplayNamesAsync(new[] { currentUser.UserId }, cancellationToken);
        var participantName = names.GetValueOrDefault(currentUser.UserId, "کاربر");

        return new LiveKitAccessDto($"group-{request.GroupId}", currentUser.UserId.ToString(), participantName);
    }
}

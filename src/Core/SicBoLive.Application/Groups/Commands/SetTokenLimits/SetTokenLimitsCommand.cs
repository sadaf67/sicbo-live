using MediatR;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;

namespace SicBoLive.Application.Groups.Commands.SetTokenLimits;

public record SetTokenLimitsCommand(Guid GroupId, decimal MinBetTokens, decimal MaxBetTokens) : IRequest;

public class SetTokenLimitsCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier) : IRequestHandler<SetTokenLimitsCommand>
{
    public async Task Handle(SetTokenLimitsCommand request, CancellationToken cancellationToken)
    {
        var group = await db.Groups.FindAsync([request.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        if (group.DealerId != currentUser.UserId)
            throw new UnauthorizedAccessException("فقط دیلر همین میز می‌تواند محدودیت‌های ژتون آن را تغییر دهد.");

        group.SetTokenLimits(request.MinBetTokens, request.MaxBetTokens);
        await db.SaveChangesAsync(cancellationToken);

        // Broadcast to the whole table so players see the new limits live instead of only
        // discovering them reactively when a bet outside the range gets rejected.
        await notifier.TokenLimitsUpdated(new TokenLimitsUpdatedDto(group.Id, group.MinBetTokens, group.MaxBetTokens), cancellationToken);
    }
}

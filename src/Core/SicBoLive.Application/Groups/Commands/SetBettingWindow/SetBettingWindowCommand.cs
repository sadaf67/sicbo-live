using MediatR;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;

namespace SicBoLive.Application.Groups.Commands.SetBettingWindow;

/// <summary>Dealer changes how many seconds players get to bet each round (Group.BettingWindowSeconds),
/// exposed from the hamburger-menu DealerPanel alongside the min/max token limits (SetTokenLimitsCommand -
/// same pattern). Only takes effect for rounds started AFTER this call: GameRound.BettingEndsAt is computed
/// once at round construction from group.BettingWindowSeconds (see StartRoundCommand) and never mutated
/// afterwards, so a round already in progress keeps its original countdown - mirrors how SetTokenLimits
/// itself only affects bets placed after the change, never retroactively.</summary>
public record SetBettingWindowCommand(Guid GroupId, int BettingWindowSeconds) : IRequest;

public class SetBettingWindowCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier)
    : IRequestHandler<SetBettingWindowCommand>
{
    public async Task Handle(SetBettingWindowCommand request, CancellationToken cancellationToken)
    {
        var group = await db.Groups.FindAsync([request.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        if (group.DealerId != currentUser.UserId)
            throw new UnauthorizedAccessException("فقط دیلر همین میز می‌تواند زمان شرط‌بندی آن را تغییر دهد.");

        group.SetBettingWindow(request.BettingWindowSeconds);
        await db.SaveChangesAsync(cancellationToken);

        await notifier.BettingWindowUpdated(new BettingWindowUpdatedDto(group.Id, group.BettingWindowSeconds), cancellationToken);
    }
}

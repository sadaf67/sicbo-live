using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.GameRounds.Commands.StartRound;

public record StartRoundCommand(Guid GroupId) : IRequest<Guid>;

public class StartRoundCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier)
    : IRequestHandler<StartRoundCommand, Guid>
{
    public async Task<Guid> Handle(StartRoundCommand request, CancellationToken cancellationToken)
    {
        var group = await db.Groups.FindAsync([request.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        if (group.DealerId != currentUser.UserId)
            throw new UnauthorizedAccessException("فقط دیلر همین میز می‌تواند دور جدید را شروع کند.");

        if (!group.IsActive)
            throw new InvalidOperationException("این میز حذف شده و دیگر فعال نیست.");

        var hasOpenRound = await db.GameRounds.AnyAsync(
            r => r.GroupId == request.GroupId && r.Status != RoundStatus.Closed, cancellationToken);
        if (hasOpenRound)
            throw new InvalidOperationException("دور قبلی هنوز بسته نشده است.");

        var roundNumber = await db.GameRounds.CountAsync(r => r.GroupId == request.GroupId, cancellationToken) + 1;
        var round = new GameRound(request.GroupId, currentUser.UserId, roundNumber, group.BettingWindowSeconds);
        db.GameRounds.Add(round);
        await db.SaveChangesAsync(cancellationToken);

        await notifier.RoundStarted(
            new RoundStartedDto(request.GroupId, round.Id, round.RoundNumber, round.BettingEndsAt), cancellationToken);

        return round.Id;
    }
}

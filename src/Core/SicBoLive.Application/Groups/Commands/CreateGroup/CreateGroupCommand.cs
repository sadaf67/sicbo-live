using MediatR;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Groups.Commands.CreateGroup;

public record CreateGroupCommand(string Name, decimal MinBetTokens, decimal MaxBetTokens, int BettingWindowSeconds, bool RestrictBigSmallOddEvenBets = false) : IRequest<Guid>;

public class CreateGroupCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser) : IRequestHandler<CreateGroupCommand, Guid>
{
    public async Task<Guid> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != UserRole.Dealer)
            throw new UnauthorizedAccessException("فقط دیلر می‌تواند میز جدید بسازد.");

        // The board paytable (BetTypeConfig) is seeded once globally at startup, not per group.
        var group = new Group(request.Name, currentUser.UserId, request.MinBetTokens, request.MaxBetTokens, request.BettingWindowSeconds, request.RestrictBigSmallOddEvenBets);
        db.Groups.Add(group);

        await db.SaveChangesAsync(cancellationToken);
        return group.Id;
    }
}

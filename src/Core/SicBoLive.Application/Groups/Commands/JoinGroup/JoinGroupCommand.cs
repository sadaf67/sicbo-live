using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Entities;

namespace SicBoLive.Application.Groups.Commands.JoinGroup;

public record JoinGroupCommand(string InviteCode) : IRequest<Guid>;

public class JoinGroupCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser) : IRequestHandler<JoinGroupCommand, Guid>
{
    public async Task<Guid> Handle(JoinGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await db.Groups.FirstOrDefaultAsync(g => g.InviteCode == request.InviteCode && g.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد یا بسته شده است.");

        // Admin-approval gate: the invite code itself is freely shareable (a dealer is meant to
        // hand it out), but redeeming it - i.e. someone actually joining a table someone else
        // pointed them to - now requires that person to already have an admin-activated
        // subscription. Without this, anyone who received the 8-character code from anyone else
        // could add themselves to a table with zero admin involvement, which is exactly the
        // unrestricted "send it and they're in" path this gate closes. The dealer who owns the
        // table is exempt - opening your own table was never an admin-gated action to begin with
        // (see CreateGroupCommand, which only requires the Dealer role).
        if (group.DealerId != currentUser.UserId)
        {
            // Note: same DateTimeOffset-comparison-in-SQL limitation as PlaceBetCommand - Sqlite
            // can't translate ExpiresAt >= now, so filter RevokedAt server-side and check
            // expiry after materializing.
            var now = DateTimeOffset.UtcNow;
            var mySubscriptions = await db.PlayerSubscriptions
                .Where(s => s.UserId == currentUser.UserId && s.RevokedAt == null)
                .ToListAsync(cancellationToken);
            if (!mySubscriptions.Any(s => s.ExpiresAt >= now))
                throw new UnauthorizedAccessException("برای پیوستن به این میز باید اشتراک شما توسط مدیر سایت فعال شود. کد دعوت به‌تنهایی کافی نیست.");
        }

        var wallet = await db.Wallets.FirstOrDefaultAsync(
            w => w.GroupId == group.Id && w.UserId == currentUser.UserId, cancellationToken);

        if (wallet is null)
        {
            wallet = new Wallet(group.Id, currentUser.UserId);
            db.Wallets.Add(wallet);
            await db.SaveChangesAsync(cancellationToken);
        }

        return group.Id;
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Groups.Commands.DeleteGroup;

public record DeleteGroupCommand(Guid GroupId) : IRequest;

public class DeleteGroupCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser) : IRequestHandler<DeleteGroupCommand>
{
    public async Task Handle(DeleteGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await db.Groups.FindAsync([request.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        if (group.DealerId != currentUser.UserId)
            throw new UnauthorizedAccessException("فقط دیلر همین میز می‌تواند آن را حذف کند.");

        var hasOpenRound = await db.GameRounds.AnyAsync(
            r => r.GroupId == request.GroupId && r.Status != RoundStatus.Closed, cancellationToken);
        if (hasOpenRound)
            throw new InvalidOperationException("نمی‌توان میزی را که دور فعال دارد حذف کرد.");

        group.Close();
        await db.SaveChangesAsync(cancellationToken);
    }
}

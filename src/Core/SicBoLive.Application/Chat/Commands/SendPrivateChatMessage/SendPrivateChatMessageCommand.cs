using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Entities;

namespace SicBoLive.Application.Chat.Commands.SendPrivateChatMessage;

public record SendPrivateChatMessageCommand(Guid GroupId, Guid RecipientId, string SenderName, string Text) : IRequest;

public class SendPrivateChatMessageCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier)
    : IRequestHandler<SendPrivateChatMessageCommand>
{
    public async Task Handle(SendPrivateChatMessageCommand request, CancellationToken cancellationToken)
    {
        if (request.RecipientId == currentUser.UserId)
            throw new ArgumentException("نمی‌توانید برای خودتان پیام خصوصی بفرستید.");

        var group = await db.Groups.FindAsync([request.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        // Only dealer + players who hold a wallet in this table count as valid DM targets -
        // prevents messaging an arbitrary user id that has nothing to do with this table.
        var recipientIsMember = group.DealerId == request.RecipientId
            || await db.Wallets.AnyAsync(w => w.GroupId == request.GroupId && w.UserId == request.RecipientId, cancellationToken);
        if (!recipientIsMember)
            throw new ArgumentException("گیرنده عضو این میز نیست.");

        var message = new ChatMessage(request.GroupId, currentUser.UserId, request.Text, request.RecipientId);
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        await notifier.PrivateChatMessagePosted(
            request.GroupId, currentUser.UserId, request.SenderName, request.RecipientId, request.Text, message.CreatedAt, cancellationToken);
    }
}

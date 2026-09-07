using MediatR;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Entities;

namespace SicBoLive.Application.Chat.Commands.SendChatMessage;

public record SendChatMessageCommand(Guid GroupId, string SenderName, string Text) : IRequest;

public class SendChatMessageCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier)
    : IRequestHandler<SendChatMessageCommand>
{
    public async Task Handle(SendChatMessageCommand request, CancellationToken cancellationToken)
    {
        var message = new ChatMessage(request.GroupId, currentUser.UserId, request.Text);
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        await notifier.ChatMessagePosted(
            request.GroupId, currentUser.UserId, request.SenderName, request.Text, message.CreatedAt, cancellationToken);
    }
}

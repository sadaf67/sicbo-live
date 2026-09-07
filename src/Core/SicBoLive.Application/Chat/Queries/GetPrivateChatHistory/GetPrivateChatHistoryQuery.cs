using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;

namespace SicBoLive.Application.Chat.Queries.GetPrivateChatHistory;

public record PrivateChatMessageDto(Guid SenderId, string SenderName, string Text, DateTimeOffset SentAt);

public record GetPrivateChatHistoryQuery(Guid GroupId, Guid OtherUserId) : IRequest<IReadOnlyList<PrivateChatMessageDto>>;

public class GetPrivateChatHistoryQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser, IUserDirectory userDirectory)
    : IRequestHandler<GetPrivateChatHistoryQuery, IReadOnlyList<PrivateChatMessageDto>>
{
    public async Task<IReadOnlyList<PrivateChatMessageDto>> Handle(GetPrivateChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var myId = currentUser.UserId;
        var otherId = request.OtherUserId;

        // No OrderBy in the EF query itself: Sqlite's provider throws NotSupportedException at
        // request time for ORDER BY over a DateTimeOffset column (CreatedAt) - materialize first,
        // then sort in memory. See CLAUDE.md "EF Core + Sqlite gotcha" for the same pattern used
        // by GetPendingTokenRequestsQuery/GetRoundHistoryQuery.
        var messages = await db.ChatMessages
            .Where(m => m.GroupId == request.GroupId &&
                ((m.SenderId == myId && m.RecipientId == otherId) ||
                 (m.SenderId == otherId && m.RecipientId == myId)))
            .ToListAsync(cancellationToken);

        var names = await userDirectory.GetDisplayNamesAsync([myId, otherId], cancellationToken);

        return messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new PrivateChatMessageDto(m.SenderId, names.GetValueOrDefault(m.SenderId, "Unknown"), m.Text, m.CreatedAt))
            .ToList();
    }
}

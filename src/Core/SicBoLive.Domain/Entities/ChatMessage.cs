using SicBoLive.Domain.Common;

namespace SicBoLive.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid GroupId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Text { get; private set; } = default!;

    /// <summary>
    /// Null for the table's public chat. Set to another member's user id for a private
    /// direct message between two members of the same table - the row is the same entity/table,
    /// just filtered differently by callers (see SendChatMessageCommand vs SendPrivateChatMessageCommand).
    /// </summary>
    public Guid? RecipientId { get; private set; }

    private ChatMessage() { }

    public ChatMessage(Guid groupId, Guid senderId, string text, Guid? recipientId = null)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("متن پیام نمی‌تواند خالی باشد.");
        if (recipientId == senderId) throw new ArgumentException("نمی‌توانید برای خودتان پیام خصوصی بفرستید.");
        GroupId = groupId;
        SenderId = senderId;
        Text = text;
        RecipientId = recipientId;
    }
}

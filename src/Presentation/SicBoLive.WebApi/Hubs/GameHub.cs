using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SicBoLive.Application.Chat.Commands.SendChatMessage;
using SicBoLive.Application.Chat.Commands.SendPrivateChatMessage;

namespace SicBoLive.WebApi.Hubs;

[Authorize]
public class GameHub(IMediator mediator) : Hub
{
    public async Task JoinGroup(Guid groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(groupId));
    }

    public async Task LeaveGroup(Guid groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(groupId));
    }

    public async Task SendChatMessage(Guid groupId, string senderName, string text)
    {
        await mediator.Send(new SendChatMessageCommand(groupId, senderName, text));
    }

    public async Task SendPrivateChatMessage(Guid groupId, Guid recipientId, string senderName, string text)
    {
        await mediator.Send(new SendPrivateChatMessageCommand(groupId, recipientId, senderName, text));
    }

    public static string GroupName(Guid groupId) => $"group:{groupId}";
}

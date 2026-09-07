using Microsoft.AspNetCore.SignalR;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.WebApi.Hubs;

namespace SicBoLive.WebApi.Services;

public class SignalRGameNotifier(IHubContext<GameHub> hub) : IGameNotifier
{
    public Task RoundStarted(RoundStartedDto round, CancellationToken cancellationToken = default) =>
        Group(round.GroupId).SendAsync("RoundStarted", round, cancellationToken);

    public Task BettingClosed(Guid groupId, Guid roundId, CancellationToken cancellationToken = default) =>
        Group(groupId).SendAsync("BettingClosed", roundId, cancellationToken);

    public Task DiceRolled(Guid groupId, Guid roundId, DiceResultDto dice, CancellationToken cancellationToken = default) =>
        Group(groupId).SendAsync("DiceRolled", roundId, dice, cancellationToken);

    public Task RoundSettled(RoundResultDto result, CancellationToken cancellationToken = default) =>
        Group(result.GroupId).SendAsync("RoundSettled", result, cancellationToken);

    public Task WalletUpdated(WalletBalanceDto wallet, CancellationToken cancellationToken = default) =>
        Group(wallet.GroupId).SendAsync("WalletUpdated", wallet, cancellationToken);

    public Task ChatMessagePosted(Guid groupId, Guid senderId, string senderName, string text, DateTimeOffset sentAt, CancellationToken cancellationToken = default) =>
        Group(groupId).SendAsync("ChatMessagePosted", new { senderId, senderName, text, sentAt }, cancellationToken);

    // Targets specific users by identity, not the table's SignalR group, so nobody else at the
    // table receives this. SignalR's default IUserIdProvider reads ClaimTypes.NameIdentifier off
    // the connection's JWT, which JwtTokenService already sets to the user's id - no extra
    // user-id-provider wiring needed for Clients.User(...) to resolve correctly here.
    public Task PrivateChatMessagePosted(Guid groupId, Guid senderId, string senderName, Guid recipientId, string text, DateTimeOffset sentAt, CancellationToken cancellationToken = default)
    {
        var payload = new { groupId, senderId, senderName, recipientId, text, sentAt };
        return Task.WhenAll(
            hub.Clients.User(senderId.ToString()).SendAsync("PrivateChatMessagePosted", payload, cancellationToken),
            hub.Clients.User(recipientId.ToString()).SendAsync("PrivateChatMessagePosted", payload, cancellationToken));
    }

    public Task TokenRequested(TokenRequestDto request, CancellationToken cancellationToken = default) =>
        Group(request.GroupId).SendAsync("TokenRequested", request, cancellationToken);

    public Task TokenRequestResolved(TokenRequestDto request, CancellationToken cancellationToken = default) =>
        Group(request.GroupId).SendAsync("TokenRequestResolved", request, cancellationToken);

    public Task TokenLimitsUpdated(TokenLimitsUpdatedDto limits, CancellationToken cancellationToken = default) =>
        Group(limits.GroupId).SendAsync("TokenLimitsUpdated", limits, cancellationToken);

    public Task BettingWindowUpdated(BettingWindowUpdatedDto window, CancellationToken cancellationToken = default) =>
        Group(window.GroupId).SendAsync("BettingWindowUpdated", window, cancellationToken);

    private IClientProxy Group(Guid groupId) => hub.Clients.Group(GameHub.GroupName(groupId));
}

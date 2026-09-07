using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SicBoLive.Application.Common.Models;
using SicBoLive.Application.Groups.Commands.CreateGroup;
using SicBoLive.Application.Groups.Commands.DeleteGroup;
using SicBoLive.Application.Groups.Commands.IssueTokens;
using SicBoLive.Application.Groups.Commands.JoinGroup;
using SicBoLive.Application.Groups.Commands.RequestTokens;
using SicBoLive.Application.Groups.Commands.RespondTokenRequest;
using SicBoLive.Application.Groups.Commands.ReturnBalanceToDealer;
using SicBoLive.Application.Groups.Commands.SetBettingWindow;
using SicBoLive.Application.Groups.Commands.SetTokenLimits;
using SicBoLive.Application.Chat.Queries.GetPrivateChatHistory;
using SicBoLive.Application.Groups.Queries.GetGroupLiveKitAccess;
using SicBoLive.Application.Groups.Queries.GetGroupMembers;
using SicBoLive.Application.Groups.Queries.GetGroupPlayers;
using SicBoLive.Application.Groups.Queries.GetGroupState;
using SicBoLive.Application.Groups.Queries.GetMyGroups;
using SicBoLive.Application.Groups.Queries.GetPendingTokenRequests;
using SicBoLive.WebApi.Services;

namespace SicBoLive.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/groups")]
public class GroupsController(IMediator mediator, LiveKitTokenService liveKitTokenService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateGroupCommand command) => Ok(await mediator.Send(command));

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<MyGroupDto>>> GetMine() =>
        Ok(await mediator.Send(new GetMyGroupsQuery()));

    [HttpPost("join")]
    public async Task<ActionResult<Guid>> Join(JoinGroupCommand command) => Ok(await mediator.Send(command));

    [HttpGet("{groupId:guid}")]
    public async Task<ActionResult<GroupStateDto>> GetState(Guid groupId) =>
        Ok(await mediator.Send(new GetGroupStateQuery(groupId)));

    [HttpDelete("{groupId:guid}")]
    public async Task<IActionResult> Delete(Guid groupId)
    {
        await mediator.Send(new DeleteGroupCommand(groupId));
        return NoContent();
    }

    [HttpGet("{groupId:guid}/players")]
    public async Task<ActionResult<IReadOnlyList<GroupPlayerDto>>> GetPlayers(Guid groupId) =>
        Ok(await mediator.Send(new GetGroupPlayersQuery(groupId)));

    [HttpGet("{groupId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<GroupMemberDto>>> GetMembers(Guid groupId) =>
        Ok(await mediator.Send(new GetGroupMembersQuery(groupId)));

    [HttpGet("{groupId:guid}/private-chat/{otherUserId:guid}")]
    public async Task<ActionResult<IReadOnlyList<PrivateChatMessageDto>>> GetPrivateChatHistory(Guid groupId, Guid otherUserId) =>
        Ok(await mediator.Send(new GetPrivateChatHistoryQuery(groupId, otherUserId)));

    [HttpPut("{groupId:guid}/token-limits")]
    public async Task<IActionResult> SetTokenLimits(Guid groupId, SetTokenLimitsBody body)
    {
        await mediator.Send(new SetTokenLimitsCommand(groupId, body.MinBetTokens, body.MaxBetTokens));
        return NoContent();
    }

    [HttpPut("{groupId:guid}/betting-window")]
    public async Task<IActionResult> SetBettingWindow(Guid groupId, SetBettingWindowBody body)
    {
        await mediator.Send(new SetBettingWindowCommand(groupId, body.BettingWindowSeconds));
        return NoContent();
    }

    [HttpPost("{groupId:guid}/players/{playerId:guid}/tokens")]
    public async Task<ActionResult<decimal>> IssueTokens(Guid groupId, Guid playerId, IssueTokensBody body) =>
        Ok(await mediator.Send(new IssueTokensCommand(groupId, playerId, body.Amount)));

    [HttpPost("{groupId:guid}/token-requests")]
    public async Task<ActionResult<Guid>> RequestTokens(Guid groupId, RequestTokensBody body) =>
        Ok(await mediator.Send(new RequestTokensCommand(groupId, body.Amount)));

    [HttpGet("{groupId:guid}/token-requests")]
    public async Task<ActionResult<IReadOnlyList<TokenRequestDto>>> GetPendingTokenRequests(Guid groupId) =>
        Ok(await mediator.Send(new GetPendingTokenRequestsQuery(groupId)));

    [HttpPost("token-requests/{requestId:guid}/respond")]
    public async Task<IActionResult> RespondTokenRequest(Guid requestId, RespondTokenRequestBody body)
    {
        await mediator.Send(new RespondTokenRequestCommand(requestId, body.Approve));
        return NoContent();
    }

    [HttpPost("{groupId:guid}/return-balance")]
    public async Task<ActionResult<decimal>> ReturnBalanceToDealer(Guid groupId) =>
        Ok(await mediator.Send(new ReturnBalanceToDealerCommand(groupId)));

    [HttpGet("{groupId:guid}/livekit-token")]
    public async Task<ActionResult<LiveKitTokenResponse>> GetLiveKitToken(Guid groupId)
    {
        var access = await mediator.Send(new GetGroupLiveKitAccessQuery(groupId));
        var result = liveKitTokenService.CreateToken(access.RoomName, access.Identity, access.ParticipantName);
        return Ok(new LiveKitTokenResponse(result.Token, result.WsUrl, result.RoomName));
    }
}

public record SetTokenLimitsBody(decimal MinBetTokens, decimal MaxBetTokens);
public record SetBettingWindowBody(int BettingWindowSeconds);
public record IssueTokensBody(decimal Amount);
public record RequestTokensBody(decimal Amount);
public record RespondTokenRequestBody(bool Approve);
public record LiveKitTokenResponse(string Token, string WsUrl, string RoomName);

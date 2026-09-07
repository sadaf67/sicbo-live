using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SicBoLive.Application.Common.Models;
using SicBoLive.Application.GameRounds.Commands.CancelBet;
using SicBoLive.Application.GameRounds.Commands.MoveBet;
using SicBoLive.Application.GameRounds.Commands.PlaceBet;
using SicBoLive.Application.GameRounds.Commands.RollDice;
using SicBoLive.Application.GameRounds.Commands.StartRound;
using SicBoLive.Application.GameRounds.Queries.GetRoundHistory;

namespace SicBoLive.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class GameRoundsController(IMediator mediator) : ControllerBase
{
    [HttpPost("groups/{groupId:guid}/rounds")]
    public async Task<ActionResult<Guid>> StartRound(Guid groupId) =>
        Ok(await mediator.Send(new StartRoundCommand(groupId)));

    [HttpGet("groups/{groupId:guid}/rounds/history")]
    public async Task<ActionResult<IReadOnlyList<RoundHistoryItemDto>>> GetRoundHistory(Guid groupId) =>
        Ok(await mediator.Send(new GetRoundHistoryQuery(groupId)));

    [HttpPost("rounds/{roundId:guid}/bets")]
    public async Task<ActionResult<Guid>> PlaceBet(Guid roundId, PlaceBetBody body) =>
        Ok(await mediator.Send(new PlaceBetCommand(roundId, body.BetTypeCode, body.Amount)));

    [HttpPost("rounds/{roundId:guid}/roll")]
    public async Task<ActionResult<RoundResultDto>> RollDice(Guid roundId) =>
        Ok(await mediator.Send(new RollDiceCommand(roundId)));

    [HttpDelete("bets/{betId:guid}")]
    public async Task<IActionResult> CancelBet(Guid betId)
    {
        await mediator.Send(new CancelBetCommand(betId));
        return NoContent();
    }

    [HttpPost("bets/{betId:guid}/move")]
    public async Task<IActionResult> MoveBet(Guid betId, MoveBetBody body)
    {
        await mediator.Send(new MoveBetCommand(betId, body.NewBetTypeCode));
        return NoContent();
    }
}

public record PlaceBetBody(string BetTypeCode, decimal Amount);
public record MoveBetBody(string NewBetTypeCode);

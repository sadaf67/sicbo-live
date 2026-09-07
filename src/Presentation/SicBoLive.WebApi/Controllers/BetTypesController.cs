using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SicBoLive.Application.BetTypes.Queries.GetBetTypes;

namespace SicBoLive.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/bet-types")]
public class BetTypesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BetTypeDto>>> GetAll() =>
        Ok(await mediator.Send(new GetBetTypesQuery()));
}

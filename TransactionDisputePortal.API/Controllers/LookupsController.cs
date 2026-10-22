using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransactionDisputePortal.Application.Features.Lookups.Models;
using TransactionDisputePortal.Application.Features.Lookups.Queries.GetLookups;

namespace TransactionDisputePortal.API.Controllers;

[ApiController]
[Route("api/v1/lookups")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class LookupsController(ISender sender) : ControllerBase
{
    private readonly ISender sender = sender;

    [HttpGet]
    [ProducesResponseType<LookupsDTO>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LookupsDTO>> GetLookups(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetLookupsQuery(), cancellationToken));
    }
}

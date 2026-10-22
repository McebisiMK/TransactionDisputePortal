using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TransactionDisputePortal.API.Authorization;
using TransactionDisputePortal.API.Extensions;
using TransactionDisputePortal.API.Models;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Application.Features.Agent.Commands.UpdateDisputeStatus;
using TransactionDisputePortal.Application.Features.Agent.Models;
using TransactionDisputePortal.Application.Features.Agent.Queries.GetAgentDisputeById;
using TransactionDisputePortal.Application.Features.Agent.Queries.GetDisputeQueue;

namespace TransactionDisputePortal.API.Controllers;

[ApiController]
[Route("api/v1/agent/disputes")]
[Authorize(Policy = Policies.Agent)]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class AgentDisputesController(ISender sender) : ControllerBase
{
    private readonly ISender sender = sender;

    [HttpGet]
    [ProducesResponseType<PaginatedList<DisputeQueueItemDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PaginatedList<DisputeQueueItemDTO>>> GetDisputeQueue([FromQuery] GetDisputeQueueQuery query, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AgentDisputeDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentDisputeDTO>> GetDisputeById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetAgentDisputeByIdQuery(id), cancellationToken));
    }

    [HttpPatch("{id:guid}/status")]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [Consumes("application/json")]
    [ProducesResponseType<AgentDisputeDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AgentDisputeDTO>> UpdateDisputeStatus(Guid id, [FromBody] UpdateDisputeStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new UpdateDisputeStatusCommand(id, request.Status, request.Comment, request.RowVersion), cancellationToken));
    }
}

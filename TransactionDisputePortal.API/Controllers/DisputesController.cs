using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransactionDisputePortal.API.Authorization;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Application.Features.Disputes.Commands.CreateDispute;
using TransactionDisputePortal.Application.Features.Disputes.Models;
using TransactionDisputePortal.Application.Features.Disputes.Queries.GetDisputeById;
using TransactionDisputePortal.Application.Features.Disputes.Queries.GetDisputes;

namespace TransactionDisputePortal.API.Controllers;

[ApiController]
[Route("api/v1/disputes")]
[Authorize(Policy = Policies.Customer)]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class DisputesController(ISender sender) : ControllerBase
{
    private readonly ISender sender = sender;

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<DisputeDTO>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DisputeDTO>> CreateDispute([FromBody] CreateDisputeCommand command, CancellationToken cancellationToken)
    {
        var dispute = await sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetDisputeById), new { id = dispute.Id }, dispute);
    }

    [HttpGet]
    [ProducesResponseType<PaginatedList<DisputeSummaryDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PaginatedList<DisputeSummaryDTO>>> GetDisputes([FromQuery] GetDisputesQuery query, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DisputeDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DisputeDTO>> GetDisputeById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetDisputeByIdQuery(id), cancellationToken));
    }
}

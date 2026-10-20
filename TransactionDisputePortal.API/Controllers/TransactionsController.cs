using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransactionDisputePortal.API.Authorization;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Application.Features.Transactions.Models;
using TransactionDisputePortal.Application.Features.Transactions.Queries.GetTransactionById;
using TransactionDisputePortal.Application.Features.Transactions.Queries.GetTransactions;

namespace TransactionDisputePortal.API.Controllers;

[ApiController]
[Route("api/v1/transactions")]
[Authorize(Policy = Policies.Customer)]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class TransactionsController(ISender sender) : ControllerBase
{
    private readonly ISender sender = sender;

    [HttpGet]
    [ProducesResponseType<PaginatedList<TransactionDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PaginatedList<TransactionDTO>>> GetTransactions([FromQuery] GetTransactionsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TransactionDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDTO>> GetTransactionById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetTransactionByIdQuery(id), cancellationToken));
    }
}

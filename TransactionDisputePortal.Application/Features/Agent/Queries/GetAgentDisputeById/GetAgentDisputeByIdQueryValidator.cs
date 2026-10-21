using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Validators;
using TransactionDisputePortal.Domain.Entities;

namespace TransactionDisputePortal.Application.Features.Agent.Queries.GetAgentDisputeById;

public class GetAgentDisputeByIdQueryValidator : AbstractValidator<GetAgentDisputeByIdQuery>
{
    private readonly IApplicationDbContext applicationDbContext;

    public GetAgentDisputeByIdQueryValidator(IApplicationDbContext applicationDbContext)
    {
        this.applicationDbContext = applicationDbContext;

        RuleFor(query => query.Id)
          .Cascade(CascadeMode.Stop)
          .NotEmpty()
          .MustAsync(Exist)
          .WithErrorCode(ValidationErrorCodes.NotFound)
          .WithMessage(query => NotFoundException.CreateMessage(nameof(Dispute), query.Id));
    }

    private Task<bool> Exist(Guid id, CancellationToken cancellationToken)
    {
        return applicationDbContext.Disputes.AnyAsync(dispute => dispute.Id == id, cancellationToken);
    }
}

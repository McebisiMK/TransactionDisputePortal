using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Validators;
using TransactionDisputePortal.Domain.Entities;

namespace TransactionDisputePortal.Application.Features.Disputes.Queries.GetDisputeById;

public class GetDisputeByIdQueryValidator : AbstractValidator<GetDisputeByIdQuery>
{
    private readonly IApplicationDbContext applicationDbContext;
    private readonly ICurrentCustomerService currentCustomerService;

    public GetDisputeByIdQueryValidator(IApplicationDbContext applicationDbContext, ICurrentCustomerService currentCustomerService)
    {
        this.applicationDbContext = applicationDbContext;
        this.currentCustomerService = currentCustomerService;

        RuleFor(query => query.Id)
          .Cascade(CascadeMode.Stop)
          .NotEmpty()
          .MustAsync(BelongToCurrentCustomer)
          .WithErrorCode(ValidationErrorCodes.NotFound)
          .WithMessage(query => NotFoundException.CreateMessage(nameof(Dispute), query.Id));
    }

    private async Task<bool> BelongToCurrentCustomer(Guid id, CancellationToken cancellationToken)
    {
        var customerId = await currentCustomerService.GetCustomerIdAsync(cancellationToken);

        return await applicationDbContext.Disputes.AnyAsync(dispute => dispute.Id == id && dispute.CustomerId == customerId, cancellationToken);
    }
}

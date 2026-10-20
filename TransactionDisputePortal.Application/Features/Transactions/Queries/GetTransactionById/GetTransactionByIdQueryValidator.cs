using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Validators;
using TransactionDisputePortal.Domain.Entities;

namespace TransactionDisputePortal.Application.Features.Transactions.Queries.GetTransactionById;

public class GetTransactionByIdQueryValidator : AbstractValidator<GetTransactionByIdQuery>
{
    private readonly IApplicationDbContext applicationDbContext;
    private readonly ICurrentCustomerService currentCustomerService;

    public GetTransactionByIdQueryValidator(IApplicationDbContext applicationDbContext, ICurrentCustomerService currentCustomerService)
    {
        this.applicationDbContext = applicationDbContext;
        this.currentCustomerService = currentCustomerService;

        RuleFor(query => query.Id)
          .Cascade(CascadeMode.Stop)
          .NotEmpty()
          .MustAsync(BelongToCurrentCustomer)
          .WithErrorCode(ValidationErrorCodes.NotFound)
          .WithMessage(query => NotFoundException.CreateMessage(nameof(Transaction), query.Id));
    }

    private async Task<bool> BelongToCurrentCustomer(Guid id, CancellationToken cancellationToken)
    {
        var customerId = await currentCustomerService.GetCustomerIdAsync(cancellationToken);

        return await applicationDbContext.Transactions.AnyAsync(transaction => transaction.Id == id && transaction.Account.CustomerId == customerId, cancellationToken);
    }
}

using FluentValidation;
using TransactionDisputePortal.Application.Common.Validators;

namespace TransactionDisputePortal.Application.Features.Transactions.Queries.GetTransactions;

public class GetTransactionsQueryValidator : AbstractValidator<GetTransactionsQuery>
{
    public GetTransactionsQueryValidator()
    {
        Include(new PagedQueryValidator());

        RuleFor(query => query.ToDate)
          .GreaterThanOrEqualTo(query => query.FromDate)
          .When(query => query.FromDate.HasValue && query.ToDate.HasValue)
          .WithMessage("'To Date' must be on or after 'From Date'.");

        RuleFor(query => query.Search)
          .MaximumLength(GetTransactionsQuery.SearchMaxLength);

        RuleFor(query => query.Status)
          .IsInEnum();
    }
}

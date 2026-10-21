using FluentValidation;
using TransactionDisputePortal.Application.Common.Validators;

namespace TransactionDisputePortal.Application.Features.Disputes.Queries.GetDisputes;

public class GetDisputesQueryValidator : AbstractValidator<GetDisputesQuery>
{
    public GetDisputesQueryValidator()
    {
        Include(new PagedQueryValidator());

        RuleFor(query => query.ToDate)
          .GreaterThanOrEqualTo(query => query.FromDate)
          .When(query => query.FromDate.HasValue && query.ToDate.HasValue)
          .WithMessage("'To Date' must be on or after 'From Date'.");

        RuleFor(query => query.Status)
          .IsInEnum();
    }
}

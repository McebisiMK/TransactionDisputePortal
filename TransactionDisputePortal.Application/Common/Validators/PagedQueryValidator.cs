using FluentValidation;
using TransactionDisputePortal.Application.Common.Models;

namespace TransactionDisputePortal.Application.Common.Validators;

public class PagedQueryValidator : AbstractValidator<PagedQuery>
{
    public PagedQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PagedQuery.MaxPageSize);
    }
}

using FluentValidation;
using TransactionDisputePortal.Application.Common.Validators;

namespace TransactionDisputePortal.Application.Features.Agent.Queries.GetDisputeQueue;

public class GetDisputeQueueQueryValidator : AbstractValidator<GetDisputeQueueQuery>
{
    public GetDisputeQueueQueryValidator()
    {
        Include(new PagedQueryValidator());

        RuleFor(query => query.Status)
          .IsInEnum();
    }
}

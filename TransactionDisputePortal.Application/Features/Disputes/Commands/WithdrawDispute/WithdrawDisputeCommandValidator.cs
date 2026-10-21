using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Validators;
using TransactionDisputePortal.Domain.Entities;

namespace TransactionDisputePortal.Application.Features.Disputes.Commands.WithdrawDispute;

public class WithdrawDisputeCommandValidator : AbstractValidator<WithdrawDisputeCommand>
{
    private readonly IApplicationDbContext applicationDbContext;
    private readonly ICurrentCustomerService currentCustomerService;

    public WithdrawDisputeCommandValidator(IApplicationDbContext applicationDbContext, ICurrentCustomerService currentCustomerService)
    {
        this.applicationDbContext = applicationDbContext;
        this.currentCustomerService = currentCustomerService;

        RuleFor(command => command.Id)
          .Cascade(CascadeMode.Stop)
          .NotEmpty()
          .MustAsync(BelongToCurrentCustomer)
          .WithErrorCode(ValidationErrorCodes.NotFound)
          .WithMessage(command => NotFoundException.CreateMessage(nameof(Dispute), command.Id));

        RuleFor(command => command.Comment)
          .MaximumLength(DisputeStatusHistory.CommentMaxLength);
    }

    private async Task<bool> BelongToCurrentCustomer(Guid id, CancellationToken cancellationToken)
    {
        var customerId = await currentCustomerService.GetCustomerIdAsync(cancellationToken);

        return await applicationDbContext.Disputes.AnyAsync(dispute => dispute.Id == id && dispute.CustomerId == customerId, cancellationToken);
    }
}

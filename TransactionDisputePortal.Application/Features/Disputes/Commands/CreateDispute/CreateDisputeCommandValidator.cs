using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Extensions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Validators;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Disputes.Commands.CreateDispute;

public class CreateDisputeCommandValidator : AbstractValidator<CreateDisputeCommand>
{
    private readonly IApplicationDbContext applicationDbContext;
    private readonly ICurrentCustomerService currentCustomerService;

    public CreateDisputeCommandValidator(IApplicationDbContext applicationDbContext, ICurrentCustomerService currentCustomerService)
    {
        this.applicationDbContext = applicationDbContext;
        this.currentCustomerService = currentCustomerService;

        RuleFor(command => command.TransactionId)
          .Cascade(CascadeMode.Stop)
          .NotEmpty()
          .MustAsync(BelongToCurrentCustomer)
          .WithErrorCode(ValidationErrorCodes.NotFound)
          .WithMessage(command => NotFoundException.CreateMessage(nameof(Transaction), command.TransactionId));

        RuleFor(command => command.Reason)
          .NotNull()
          .IsInEnum();

        RuleFor(command => command.Description)
          .Must(description => description.IsNotBlank())
          .When(command => command.Reason == DisputeReason.Other)
          .WithMessage("'Description' is required when the reason is 'Other'.");

        RuleFor(command => command.Description)
          .MaximumLength(Dispute.DescriptionMaxLength);
    }

    private async Task<bool> BelongToCurrentCustomer(Guid transactionId, CancellationToken cancellationToken)
    {
        var customerId = await currentCustomerService.GetCustomerIdAsync(cancellationToken);

        return await applicationDbContext.Transactions.AnyAsync(transaction => transaction.Id == transactionId && transaction.Account.CustomerId == customerId, cancellationToken);
    }
}

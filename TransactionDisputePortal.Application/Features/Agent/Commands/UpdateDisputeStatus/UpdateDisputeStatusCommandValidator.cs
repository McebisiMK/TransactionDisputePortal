using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Extensions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Validators;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Agent.Commands.UpdateDisputeStatus;

public class UpdateDisputeStatusCommandValidator : AbstractValidator<UpdateDisputeStatusCommand>
{
    private readonly IApplicationDbContext applicationDbContext;

    public UpdateDisputeStatusCommandValidator(IApplicationDbContext applicationDbContext)
    {
        this.applicationDbContext = applicationDbContext;

        RuleFor(command => command.Id)
          .Cascade(CascadeMode.Stop)
          .NotEmpty()
          .MustAsync(Exist)
          .WithErrorCode(ValidationErrorCodes.NotFound)
          .WithMessage(command => NotFoundException.CreateMessage(nameof(Dispute), command.Id));

        RuleFor(command => command.Status)
          .Cascade(CascadeMode.Stop)
          .NotNull()
          .IsInEnum()
          .Must(status => UpdateDisputeStatusCommand.AllowedStatuses.Contains(status!.Value))
          .WithMessage($"'Status' must be one of: {string.Join(", ", UpdateDisputeStatusCommand.AllowedStatuses)}.");

        RuleFor(command => command.Comment)
          .Must(comment => comment.IsNotBlank())
          .When(command => command.Status is DisputeStatus.Approved or DisputeStatus.Rejected)
          .WithMessage("'Comment' is required when resolving a dispute.");

        RuleFor(command => command.Comment)
          .MaximumLength(DisputeStatusHistory.CommentMaxLength);

        RuleFor(command => command.RowVersion)
          .NotNull();
    }

    private Task<bool> Exist(Guid id, CancellationToken cancellationToken)
    {
        return applicationDbContext.Disputes.AnyAsync(dispute => dispute.Id == id, cancellationToken);
    }
}

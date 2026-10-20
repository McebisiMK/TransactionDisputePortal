using MediatR;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = TransactionDisputePortal.Application.Common.Exceptions.ValidationException;

namespace TransactionDisputePortal.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidationBehavior<TRequest, TResponse>> logger)
 : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> validators = validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(result => result.Errors).Where(failure => failure is not null).ToList();

            if (failures.Count != 0)
            {
                logger.LogWarning("Validation failed for {RequestType} with {ErrorCount} error(s): {Errors}", typeof(TRequest).Name, failures.Count, failures.Select(f => f.ErrorMessage));
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}

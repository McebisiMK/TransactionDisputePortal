using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TransactionDisputePortal.API.Models;
using TransactionDisputePortal.Application.Common.Exceptions;
using ValidationException = TransactionDisputePortal.Application.Common.Exceptions.ValidationException;

namespace TransactionDisputePortal.API.ExceptionHandling;

public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly IHostEnvironment environment = environment;
    private readonly ILogger<GlobalExceptionHandler> logger = logger;
    private readonly IProblemDetailsService problemDetailsService = problemDetailsService;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var path = httpContext.Request.Path.Value;

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request {Path} was cancelled by the client", path);
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var problemDetails = exception switch
        {
            ValidationException ex => CreateValidationProblem(ex),
            NotFoundException ex => CreateProblem(StatusCodes.Status404NotFound, "Not Found", ex.Message, "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ForbiddenAccessException ex => CreateProblem(StatusCodes.Status403Forbidden, "Forbidden", ex.Message, "https://tools.ietf.org/html/rfc9110#section-15.5.4"),
            ConcurrencyException ex => CreateProblem(StatusCodes.Status409Conflict, "Conflict", ex.Message, "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            _ => CreateProblem(StatusCodes.Status500InternalServerError, "Internal Server Error", environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.", "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };

        LogException(exception, problemDetails.Status!.Value, path);

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static HttpValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var details = new HttpValidationProblemDetails(exception.Errors)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.21"
        };

        details.Extensions["fieldErrors"] = exception.Errors.Select(keyValuePair => new FieldValidationError(keyValuePair.Key, keyValuePair.Value)).ToArray();

        return details;
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail, string type)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = type
        };
    }

    private void LogException(Exception exception, int status, string? path)
    {
        switch (exception)
        {
            case ValidationException validationException:
                logger.LogInformation("Validation error on {Path}: {@Errors}", path, validationException.Errors);
                break;
            case NotFoundException:
                logger.LogInformation("Resource not found on {Path}: {Message}", path, exception.Message);
                break;
            case ForbiddenAccessException or ConcurrencyException:
                logger.LogWarning("Request to {Path} failed with {StatusCode}: {Message}", path, status, exception.Message);
                break;
            default:
                logger.LogError(exception, "Unhandled exception occurred while processing {Path}", path);
                break;
        }
    }
}

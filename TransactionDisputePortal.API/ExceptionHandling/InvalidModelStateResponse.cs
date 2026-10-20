using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using TransactionDisputePortal.API.Helpers;
using TransactionDisputePortal.API.Models;

namespace TransactionDisputePortal.API.ExceptionHandling;

/// <summary>
/// Returns model binding failures (e.g. an unknown enum value or a malformed date) with the same
/// 422 shape the <see cref="GlobalExceptionHandler"/> uses for FluentValidation failures.
/// </summary>
public static class InvalidModelStateResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var problemDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

        var details = problemDetailsFactory.CreateValidationProblemDetails(
            context.HttpContext,
            context.ModelState,
            StatusCodes.Status422UnprocessableEntity,
            "One or more validation errors occurred.",
            "https://tools.ietf.org/html/rfc9110#section-15.5.21");

        details.Extensions["fieldErrors"] = details.Errors.Select(keyValuePair => new FieldValidationError(keyValuePair.Key, keyValuePair.Value)).ToArray();

        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(InvalidModelStateResponse));
        logger.LogInformation("Model binding failed on {Path}: {@Errors}", context.HttpContext.Request.Path.Value, details.Errors);

        return ProblemJsonResult.Create(details);
    }
}

using Microsoft.AspNetCore.Mvc;

namespace TransactionDisputePortal.API.Helpers;

public static class ProblemJsonResult
{
    // JsonResult rather than ObjectResult so [Produces("application/json")] on controllers doesn't override the problem content type.
    public static JsonResult Create(ProblemDetails details) => new(details)
    {
        StatusCode = details.Status,
        ContentType = "application/problem+json"
    };
}

using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace TransactionDisputePortal.API.Extensions;

public static class RateLimitingExtensions
{
    public const string WritePolicy = "writes";

    public static void RegisterRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:Writes:PermitLimit", 10);
        var window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Writes:WindowSeconds", 60));

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(WritePolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                GetPartitionKey(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0
                }));

            options.OnRejected = WriteRejectionAsync;
        });
    }

    private static string GetPartitionKey(HttpContext httpContext)
    {
        var subject = httpContext.User.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(subject))
            return $"user:{subject}";

        return $"ip:{httpContext.Connection.RemoteIpAddress}";
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(RateLimitingExtensions));
        logger.LogWarning("Rate limit exceeded on {Path}", httpContext.Request.Path.Value);

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "Too many changes in a short time. Please wait a moment and try again.",
                Type = "https://tools.ietf.org/html/rfc6585#section-4"
            }
        });
    }
}

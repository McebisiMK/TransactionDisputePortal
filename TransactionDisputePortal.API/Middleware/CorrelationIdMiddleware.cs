using System.Diagnostics;
using Serilog.Context;

namespace TransactionDisputePortal.API.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string LogPropertyName = "CorrelationId";

    private const int MaxLength = 64;

    private readonly RequestDelegate next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(LogPropertyName, correlationId))
        {
            await next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();

        if (IsValid(incoming))
            return incoming;

        return Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }

    private static bool IsValid(string value)
    {
        return value.Length is > 0 and <= MaxLength && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
    }
}

namespace TransactionDisputePortal.API.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string ApiContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

    private readonly RequestDelegate next = next;

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            if (!context.Request.Path.StartsWithSegments("/swagger"))
                headers.ContentSecurityPolicy = ApiContentSecurityPolicy;

            if (context.Request.Path.StartsWithSegments("/api"))
                headers.CacheControl = "no-store";

            return Task.CompletedTask;
        });

        return next(context);
    }
}

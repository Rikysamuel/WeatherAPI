namespace WeatherApi.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers.XXSSProtection = "0";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        if (context.Request.IsHttps)
        {
            context.Response.Headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
        }

        await _next(context);
    }
}

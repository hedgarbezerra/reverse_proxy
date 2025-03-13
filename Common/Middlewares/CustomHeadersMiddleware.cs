using Microsoft.AspNetCore.Http;

namespace Common.Middlewares;

public class CustomHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public CustomHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add X-Request-Path header
        context.Request.Headers["X-Request-Path"] = context.Request.Path.Value;

        // Add X-Datadog-Trace-Id header
        var traceId = context.TraceIdentifier;
        context.Request.Headers["X-Datadog-Trace-Id"] = traceId;

        // Add or keep RequestTraceId header
        if (!context.Request.Headers.ContainsKey("RequestTraceId"))
        {
            context.Request.Headers["RequestTraceId"] = traceId;
        }

        await _next(context);
    }
}
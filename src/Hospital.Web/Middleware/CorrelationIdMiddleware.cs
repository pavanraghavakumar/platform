using System.Diagnostics;

namespace Hospital.Web.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;
        context.TraceIdentifier = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await next(context);
    }
}

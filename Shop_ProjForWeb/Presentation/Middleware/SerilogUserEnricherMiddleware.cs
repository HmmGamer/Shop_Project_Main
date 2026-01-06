using Serilog.Context;
using System.Security.Claims;

namespace Shop_ProjForWeb.Presentation.Middleware;

public class SerilogUserEnricherMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SerilogUserEnricherMiddleware> _logger;

    public SerilogUserEnricherMiddleware(RequestDelegate next, ILogger<SerilogUserEnricherMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            string? userId = null;
            string? userName = null;
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                userName = context.User.FindFirst(ClaimTypes.Name)?.Value;
            }

            using (LogContext.PushProperty("UserId", userId ?? "anonymous"))
            using (LogContext.PushProperty("UserName", userName ?? "anonymous"))
            using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
            {
                await _next(context);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SerilogUserEnricherMiddleware");
            throw;
        }
    }
}

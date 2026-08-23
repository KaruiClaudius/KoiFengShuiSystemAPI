using KoiFengShuiSystem.Shared.Infrastructure.Background;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Host.Middleware;

/// <summary>
/// Records one traffic log entry per request. The downstream pipeline always
/// runs first; the entry is then built from the final response state and handed
/// to the background sink, so request latency never includes database I/O and
/// requests that fail before completing are not logged.
/// </summary>
public class TrafficLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITrafficSink _sink;
    private readonly ILogger<TrafficLoggingMiddleware> _logger;

    public TrafficLoggingMiddleware(RequestDelegate next, ILogger<TrafficLoggingMiddleware> logger, ITrafficSink sink)
    {
        _next = next;
        _logger = logger;
        _sink = sink;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        try
        {
            _sink.Enqueue(BuildEntry(context));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing traffic log entry");
        }
    }

    private static TrafficLogEntry BuildEntry(HttpContext context)
    {
        var isRegistered = context.User?.Identity?.IsAuthenticated ?? false;
        int? accountId = null;

        if (isRegistered)
        {
            var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedId))
            {
                accountId = parsedId;
            }
        }

        return new TrafficLogEntry
        {
            Timestamp = DateTime.UtcNow,
            StatusCode = context.Response.StatusCode,
            IpAddress = context.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers["User-Agent"].ToString(),
            RequestPath = context.Request.Path.ToString(),
            RequestMethod = context.Request.Method,
            IsRegistered = isRegistered,
            AccountId = accountId
        };
    }
}

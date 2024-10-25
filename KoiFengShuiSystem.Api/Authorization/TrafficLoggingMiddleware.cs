using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Helpers;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Api.Authorization
{
    public class TrafficLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TrafficLoggingMiddleware> _logger;

        public TrafficLoggingMiddleware(RequestDelegate next, ILogger<TrafficLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, KoiFengShuiContext dbContext)
        {
            try
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

                var log = new TrafficLog
                {
                    Timestamp = DateTime.UtcNow,
                    IpAddress = context.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    RequestPath = context.Request.Path.ToString(),
                    RequestMethod = context.Request.Method,
                    IsRegistered = isRegistered,
                    AccountId = accountId
                };

                dbContext.TrafficLogs.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrafficLoggingMiddleware");
                // Don't throw the exception, just log it and continue
            }

            await _next(context);
        }
    }
}

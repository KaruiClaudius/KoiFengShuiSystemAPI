using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Helpers;
using System.Security.Claims;

namespace KoiFengShuiSystem.Api.Authorization
{
    public class TrafficLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public TrafficLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, KoiFengShuiContext dbContext)
        {
            var isRegistered = context.User.Identity.IsAuthenticated;
            var userId = isRegistered ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value : null;

            var log = new TrafficLog
            {
                Timestamp = DateTime.UtcNow,
                IsRegistered = isRegistered,
                AccountId = isRegistered ? int.Parse(userId) : null,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                RequestPath = context.Request.Path,
                RequestMethod = context.Request.Method
            };

            dbContext.TrafficLogs.Add(log);
            await dbContext.SaveChangesAsync();

            await _next(context);
        }
    }
}

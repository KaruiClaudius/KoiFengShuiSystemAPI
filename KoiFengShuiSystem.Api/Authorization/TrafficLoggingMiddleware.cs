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
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                RequestPath = context.Request.Path,
                RequestMethod = context.Request.Method,
                IsRegistered = context.User.Identity.IsAuthenticated,
                AccountId = context.User.Identity.IsAuthenticated ?
                int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier).Value) : (int?)null
            };

            dbContext.TrafficLogs.Add(log);
            await dbContext.SaveChangesAsync();

            await _next(context);
        }
    }
}

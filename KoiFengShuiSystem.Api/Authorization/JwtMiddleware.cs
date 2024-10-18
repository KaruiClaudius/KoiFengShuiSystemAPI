using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using Microsoft.Extensions.Logging;
using System.Net;

namespace KoiFengShuiSystem.Api.Authorization
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, IAccountService accountService, IJwtUtils jwtUtils)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var accountId = jwtUtils.ValidateJwtToken(token);
                    if (accountId != null)
                    {
                        // Attach user to context on successful jwt validation
                        context.Items["Account"] = await accountService.GetByIdAsync(accountId.Value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating JWT token");
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync("Invalid token");
                    return;
                }
            }

            await _next(context);
        }
    }
}

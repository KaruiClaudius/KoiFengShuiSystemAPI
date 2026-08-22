using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
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

        public async Task Invoke(HttpContext context, IAccountService accountService, IJwtTokenService jwtTokenService)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            int? accountId = null;

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    accountId = jwtTokenService.ValidateJwtToken(token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating JWT token");
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync("Invalid token");
                    return;
                }

                if (accountId != null)
                {
                    // Attach user to context on successful jwt validation
                    context.Items["Account"] = await accountService.GetByIdAsync(accountId.Value);
                }
            }

            await _next(context);
        }
    }
}

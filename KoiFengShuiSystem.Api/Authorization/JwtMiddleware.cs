using KoiFengShuiSystem.BusinessLogic.Services.Interface;

namespace KoiFengShuiSystem.Api.Authorization
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IAccountService accountService, IJwtUtils jwtUtils)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var accountId = jwtUtils.ValidateJwtToken(token);
            if (accountId != null)
            {
                // attach user to context on successful jwt validation
                context.Items["Account"] = accountService.GetById(accountId.Value);
            }

            await _next(context);
        }
    }
}

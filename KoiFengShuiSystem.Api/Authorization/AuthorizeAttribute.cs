using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using AccountEntity = KoiFengShuiSystem.Modules.Identity.Domain.Entities.Account;

namespace KoiFengShuiSystem.Api.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // skip authorization if action is decorated with [AllowAnonymous] attribute
            var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
            if (allowAnonymous)
                return;

            // authorization
            var account = (AccountEntity?)context.HttpContext.Items["Account"];
            if (account == null)
            {
                // not logged in or role not authorized
                context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            }
        }
    }
}

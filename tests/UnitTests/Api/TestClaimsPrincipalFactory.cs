using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UnitTests.Api
{
    /// <summary>
    /// Builds ClaimsPrincipal instances that mirror the claims minted by
    /// JwtTokenService ("id", ClaimTypes.Email, ClaimTypes.Role).
    /// </summary>
    public static class TestClaimsPrincipalFactory
    {
        public static ClaimsPrincipal CreateAccount(int accountId, int roleId)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("id", accountId.ToString()),
                new Claim(ClaimTypes.Role, roleId.ToString())
            }, authenticationType: "Test"));
        }

        public static void AttachAccountId(ControllerBase controller, int accountId)
        {
            controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = CreateAccount(accountId, roleId: 2)
            };
        }
    }
}

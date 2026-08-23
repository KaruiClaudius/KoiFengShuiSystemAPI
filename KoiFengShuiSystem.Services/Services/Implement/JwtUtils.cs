using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KoiFengShuiSystem.Shared.Helpers;
using AccountEntity = KoiFengShuiSystem.Modules.Identity.Domain.Entities.Account;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public interface IJwtUtils
    {
        public string GenerateJwtToken(AccountEntity account);
        public int? ValidateJwtToken(string? token);

    }

    public class JwtUtils : IJwtUtils
    {
        private readonly AppSettings _appSettings;
        private readonly IJwtTokenService _jwtTokenService;

        public JwtUtils(IOptions<AppSettings> appSettings, IJwtTokenService jwtTokenService)
        {
            _appSettings = appSettings.Value;
            _jwtTokenService = jwtTokenService;

            if (string.IsNullOrEmpty(_appSettings.Secret))
                throw new Exception("JWT secret not configured");
        }

        public string GenerateJwtToken(AccountEntity account)
        {
            // generate token that is valid for 7 days
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret!);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", account.AccountId.ToString()),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim(ClaimTypes.Role, account.RoleId.ToString())
            }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public int? ValidateJwtToken(string? token)
        {
            // Legacy validator: delegates to the shared identity token service so token
            // validation parameters are built by the single TokenValidationParametersFactory.
            return _jwtTokenService.ValidateJwtToken(token);
        }

    }
}

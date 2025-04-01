using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace ApiServer.Services
{
    public class JwtTokenGenerator
    {
        private readonly string jwtKey;
        private readonly SigningCredentials credentials;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            jwtKey = configuration.GetValue<string>("Auth:JwtKey");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        public string GenerateJwt(Account account)
            => GenerateJwt(account.Login, account.AccountId);
        public string GenerateJwt(string login, string accountId)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, accountId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, accountId),
                new Claim(ClaimTypes.Surname, login)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

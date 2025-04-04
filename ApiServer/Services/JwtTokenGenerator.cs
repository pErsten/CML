using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace ApiServer.Services
{
    /// <summary>
    /// Service for generating JWT tokens for authenticated users. 
    /// Tokens include claims for account ID and login name.
    /// </summary>
    public class JwtTokenGenerator
    {
        private readonly string jwtKey;
        private readonly SigningCredentials credentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenGenerator"/> class using configuration to retrieve the secret signing key.
        /// </summary>
        public JwtTokenGenerator(IConfiguration configuration)
        {
            jwtKey = configuration.GetValue<string>("Auth:JwtKey");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        /// <summary>
        /// Generates a JWT token for the given account.
        /// </summary>
        /// <param name="account">The user account to generate a token for.</param>
        /// <returns>A JWT token as a string.</returns>
        public string GenerateJwt(Account account)
            => GenerateJwt(account.Login, account.AccountId);

        /// <summary>
        /// Generates a JWT token using the specified login and account ID as claims.
        /// </summary>
        /// <param name="login">The user's login name.</param>
        /// <param name="accountId">The user's unique account ID.</param>
        /// <returns>A signed JWT token as a string.</returns>
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

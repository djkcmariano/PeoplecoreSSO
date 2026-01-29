using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthServer.Helpers;   

namespace AuthServer.Helpers
{
    public class JwtHelper
    {
        private readonly string _secret;

        public JwtHelper(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentException("JWT secret is required.", nameof(secret));

            _secret = PeopleCoreCrypt.Decrypt(secret);
        }

        public SymmetricSecurityKey GetSigningKey()
        {
            var keyBytes = Encoding.UTF8.GetBytes(_secret);
            return new SymmetricSecurityKey(keyBytes);
        }

        public string GenerateToken(ClaimsIdentity identity, string issuer, string audience, int expireHours = 1)
        {
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: identity.Claims,
                expires: DateTime.UtcNow.AddHours(expireHours),
                signingCredentials: new SigningCredentials(
                    GetSigningKey(),
                    SecurityAlgorithms.HmacSha256
                )
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

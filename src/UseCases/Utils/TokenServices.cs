using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Domain.Entities;
using Domain.Shared;

using Microsoft.IdentityModel.Tokens;

namespace UseCases.Utils;

public class TokenService(JwtSettings jwtSettings)
{
    public string GenerateAccessToken(User user)
    {

        string secretKey = jwtSettings.SecretKey;
        string issuer = jwtSettings.Issuer;
        string audience = jwtSettings.Audience;
        var expirationMinutes = jwtSettings.TokenExpirationInMinutes;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}

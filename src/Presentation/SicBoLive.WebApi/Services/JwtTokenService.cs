using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SicBoLive.Infrastructure.Identity;

namespace SicBoLive.WebApi.Services;

public class JwtTokenService(IConfiguration configuration)
{
    public string CreateToken(ApplicationUser user)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            // Single-active-session enforcement: caller must have already refreshed
            // user.CurrentSessionId to a new Guid and persisted it (UpdateAsync) *before* calling
            // this method, so the claim baked into this token matches what's now in the DB. See
            // Program.cs's OnTokenValidated - any older token with a stale "sid" gets rejected the
            // instant a newer login/switch-role happens, so two devices can never both hold a
            // valid token for the same account at once.
            new Claim("sid", user.CurrentSessionId.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(double.Parse(jwtSection["ExpiryHours"] ?? "12")),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

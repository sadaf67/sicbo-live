using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SicBoLive.WebApi.Services;

public record LiveKitTokenResult(string Token, string WsUrl, string RoomName);

public class LiveKitTokenService(IConfiguration configuration)
{
    public LiveKitTokenResult CreateToken(string roomName, string identity, string participantName)
    {
        var section = configuration.GetSection("LiveKit");
        var apiKey = section["ApiKey"]!;
        var apiSecret = section["ApiSecret"]!;
        var wsUrl = section["WsUrl"]!;
        var ttlMinutes = double.Parse(section["TokenTtlMinutes"] ?? "180");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(apiSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTimeOffset.UtcNow;
        var videoGrant = new Dictionary<string, object>
        {
            ["room"] = roomName,
            ["roomJoin"] = true,
            ["canPublish"] = true,
            ["canSubscribe"] = true,
            ["canPublishData"] = true,
        };

        var payload = new JwtPayload
        {
            { "iss", apiKey },
            { "sub", identity },
            { "name", participantName },
            { "nbf", now.ToUnixTimeSeconds() },
            { "exp", now.AddMinutes(ttlMinutes).ToUnixTimeSeconds() },
            { "video", videoGrant },
        };

        var header = new JwtHeader(credentials);
        var token = new JwtSecurityToken(header, payload);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return new LiveKitTokenResult(jwt, wsUrl, roomName);
    }
}

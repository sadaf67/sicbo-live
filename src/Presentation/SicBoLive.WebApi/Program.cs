using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SicBoLive.Application;
using SicBoLive.Infrastructure;
using SicBoLive.Infrastructure.Persistence;
using SicBoLive.WebApi.Hubs;
using SicBoLive.WebApi.Services;
using SicBoLive.Application.Common.Interfaces;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "Client";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SicBo Live API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IGameNotifier, SignalRGameNotifier>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<LiveKitTokenService>();
builder.Services.AddHostedService<TelegramBotService>();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(builder.Configuration.GetSection("ClientOrigins").Get<string[]>() ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!)),
        };

        // Let SignalR clients authenticate via ?access_token=... since browsers can't set headers on WS handshakes.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            },
            // Single-active-session enforcement (anti account-sharing control - see ApplicationUser.
            // CurrentSessionId doc comment): signature/expiry alone aren't enough, because nothing else
            // re-checks a validated token against the database afterwards (every claim read elsewhere,
            // e.g. ICurrentUserService, trusts the JWT blindly). This hook is the one place that does
            // that DB check, on every single authenticated request (REST) and SignalR handshake: if the
            // token's "sid" claim doesn't match the user's *current* CurrentSessionId (i.e. someone
            // logged in elsewhere since this token was issued), the request is rejected with 401 even
            // though the token itself is still validly signed and unexpired.
            OnTokenValidated = async context =>
            {
                var sidClaim = context.Principal?.FindFirst("sid")?.Value;
                var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (sidClaim is null || userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("توکن نامعتبر است.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<SicBoLive.Infrastructure.Identity.ApplicationUser>>();
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user is null || user.CurrentSessionId.ToString() != sidClaim)
                {
                    context.Fail("این حساب از دستگاه یا مرورگر دیگری وارد شده است. لطفاً دوباره وارد شوید.");
                }
            },
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<SicBoLive.Infrastructure.Identity.ApplicationUser>>();
    await ApplicationDbContextSeeder.SeedAsync(db, userManager);
}

app.UseMiddleware<SicBoLive.WebApi.Middleware.ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Runs behind nginx (reverse proxy on the host) which terminates TLS - without this middleware,
// UseHttpsRedirection would redirect-loop because Kestrel sees every request as plain HTTP. Must
// be registered before UseHttpsRedirection.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();
// Serves the built React client (client/vite.config.ts's build.outDir points here) so the
// container is a single origin in production - CORS below stays enabled too since it's still
// needed for local dev (Vite on :5173 talking to the API on :5299).
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/hubs/game");
app.MapFallbackToFile("index.html");

app.Run();

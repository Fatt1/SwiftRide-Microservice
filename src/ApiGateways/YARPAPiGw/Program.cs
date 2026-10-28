using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Extensions;
using Yarp.ReverseProxy.Transforms;
using YARPAPiGw.Configurations;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry Tracing & Metrics
var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "YarpApiGw";
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
builder.Services.AddCustomOpenTelemetry(serviceName, otlpEndpoint);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// CORS configuration (allow requests from frontend / mobile clients, including SignalR WebSockets)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// JWT Settings & Authentication (identical to Identity.API)
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization policy used by YARP routes (requires valid JWT token)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
});

// Reverse Proxy (YARP) with user claim propagation headers
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(transformContext =>
        {
            var user = transformContext.HttpContext.User;
            if (user.Identity is { IsAuthenticated: true })
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    transformContext.ProxyRequest.Headers.Remove("X-User-Id");
                    transformContext.ProxyRequest.Headers.Add("X-User-Id", userId);
                }

                var email = user.FindFirst(ClaimTypes.Email)?.Value
                    ?? user.FindFirst("email")?.Value;

                if (!string.IsNullOrEmpty(email))
                {
                    transformContext.ProxyRequest.Headers.Remove("X-User-Email");
                    transformContext.ProxyRequest.Headers.Add("X-User-Email", email);
                }

                var userName = user.FindFirst(ClaimTypes.Name)?.Value
                    ?? user.FindFirst("unique_name")?.Value;

                if (!string.IsNullOrEmpty(userName))
                {
                    transformContext.ProxyRequest.Headers.Remove("X-User-Name");
                    transformContext.ProxyRequest.Headers.Add("X-User-Name", userName);
                }
            }

            return ValueTask.CompletedTask;
        });
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

// Root Gateway info endpoint
app.MapGet("/", () => Results.Ok(new
{
    Service = "SwiftRide API Gateway (YARP)",
    Status = "Healthy",
    Version = "1.0",
    Timestamp = DateTime.UtcNow,
    Endpoints = new
    {
        Auth = "/api/v1/auth (Identity.API)",
        Matching = "/api/v1/matching (Matching.API - Protected)",
        Payment = "/api/v1/payments (Payment.API - Protected)",
        Trip = "/api/v1/trips (Trip.API - Protected)"
    }
}));

// Map YARP Reverse Proxy
app.MapReverseProxy();

await app.RunAsync();

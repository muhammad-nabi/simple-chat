using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using SimpleChat.Infrastructure.Data;
using SimpleChat.Web.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

// Stage 1: Bootstrap logger for startup errors
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Stage 2: Full Serilog configuration — replaces default logging
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .WriteTo.Console(new RenderedCompactJsonFormatter()));

// JWT secret validation — warn if using default placeholder
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (jwtSecret == "CHANGE-THIS-IN-PRODUCTION-min-32-chars!!")
{
    Log.Warning("JWT secret is set to the default placeholder value. Change Jwt:Secret before deploying to production.");
}

// Add services to the container.
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

// Rate limiting for auth endpoints
var loginPerMinutePerIp = builder.Configuration.GetValue("RateLimit:LoginPerMinutePerIp", 20);
var loginPerMinutePerUser = builder.Configuration.GetValue("RateLimit:LoginPerMinutePerUser", 5);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Per-IP rate limiting for all auth endpoints
    options.AddPolicy("auth", httpContext =>
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"ip:{remoteIp}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPerMinutePerIp,
                Window = TimeSpan.FromMinutes(1),
            });
    });

    // Per-email rate limiting for login endpoint only (AC4: 5 attempts/min per username)
    options.AddPolicy("auth-per-user", httpContext =>
    {
        var email = "unknown";

        try
        {
            httpContext.Request.EnableBuffering();
            httpContext.Request.Body.Position = 0;
            using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
            var body = reader.ReadToEndAsync().GetAwaiter().GetResult();
            httpContext.Request.Body.Position = 0;

            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("email", out var emailProp))
            {
                email = emailProp.GetString()?.ToLowerInvariant() ?? "unknown";
            }
        }
        catch
        {
            // If body parsing fails, fall back to "unknown" partition
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"user:{email}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPerMinutePerUser,
                Window = TimeSpan.FromMinutes(1),
            });
    });
});

var app = builder.Build();

// Database initialization — runs migrations in all environments for zero-ops upgrades (FR41, FR42)
try
{
    await app.InitialiseDatabaseAsync();
    app.Services.GetRequiredService<StartupHealthCheck>().MarkReady();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Database initialization failed. The application will start but /health/startup will report unhealthy.");
}

// Health check endpoint — mapped early so it responds even during app errors
app.MapHealthChecks("/health/startup", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("startup")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(result);
    }
});

// Serilog request logging — placed after health endpoints to avoid noisy healthcheck logs
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// CORS — use configured origins in production, allow any in development
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
app.UseCors(corsBuilder =>
{
    corsBuilder.AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();

    if (allowedOrigins is { Length: > 0 })
    {
        corsBuilder.WithOrigins(allowedOrigins);
    }
    else if (app.Environment.IsDevelopment())
    {
        corsBuilder.SetIsOriginAllowed(_ => true);
    }
    else
    {
        Log.Warning("No CORS origins configured (Cors:AllowedOrigins). Only same-origin requests will work.");
        corsBuilder.WithOrigins("https://localhost");
    }
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseFileServer();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler(options => { });


app.MapEndpoints(typeof(Program).Assembly);

app.MapFallbackToFile("index.html");

app.Run();

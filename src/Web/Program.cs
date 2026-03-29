using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SimpleChat.Infrastructure.Data;
using SimpleChat.Web.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(static builder =>
    builder.AllowAnyMethod()
        .AllowAnyHeader()
        .AllowAnyOrigin());

app.UseFileServer();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler(options => { });


app.MapEndpoints(typeof(Program).Assembly);

app.MapFallbackToFile("index.html");

app.Run();

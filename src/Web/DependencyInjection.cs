using Azure.Identity;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Infrastructure.Data;
using SimpleChat.Web.HealthChecks;
using SimpleChat.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUser>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApi(options =>
        {
            options.AddOperationTransformer<ApiExceptionOperationTransformer>();
            options.AddOperationTransformer<IdentityApiOperationTransformer>();
        });

        builder.Services.AddCors();

        // Health checks — singleton first so MarkReady() and health check use the same instance
        builder.Services.AddSingleton<StartupHealthCheck>();
        var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
        builder.Services.AddHealthChecks()
            .AddCheck<StartupHealthCheck>("startup", tags: new[] { "startup" })
            .AddCheck<LivenessHealthCheck>("liveness", tags: new[] { "live" })
            .AddDbContextCheck<ApplicationDbContext>("database", tags: new[] { "ready" })
            .AddRedis(redisConnectionString, name: "redis", tags: new[] { "ready" });
    }

    public static void AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }
    }
}

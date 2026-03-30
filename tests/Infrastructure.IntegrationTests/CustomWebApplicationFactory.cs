using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleChat.Infrastructure.Data;
using StackExchange.Redis;

namespace SimpleChat.Infrastructure.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override configuration so health checks and DI read the Testcontainer endpoints
        builder.UseSetting("ConnectionStrings:SimpleChatDb", TestcontainersFixture.MsSqlConnectionString);
        builder.UseSetting("Redis:ConnectionString", TestcontainersFixture.RedisConnectionString);
        builder.UseSetting("Jwt:Secret", "integration-test-secret-must-be-at-least-32-chars!!");
        builder.UseSetting("Jwt:ExpiryMinutes", "30");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor);

            // Remove existing Redis registration
            var redisDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor is not null)
                services.Remove(redisDescriptor);

            // Add DbContext with Testcontainers MSSQL
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseSqlServer(TestcontainersFixture.MsSqlConnectionString);
            });

            // Add Redis with Testcontainers Redis
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(TestcontainersFixture.RedisConnectionString));
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitialiseDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }
}

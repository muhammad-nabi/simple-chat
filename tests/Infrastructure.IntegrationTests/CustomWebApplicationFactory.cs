using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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

            // Relax password rules for test environment
            services.Configure<IdentityOptions>(opts =>
            {
                opts.Password.RequireDigit = false;
                opts.Password.RequireUppercase = false;
                opts.Password.RequireLowercase = false;
                opts.Password.RequireNonAlphanumeric = false;
                opts.Password.RequiredLength = 6;
            });
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitialiseDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        // Dynamically clear all tables (except migration history) regardless of schema
        await context.Database.ExecuteSqlRawAsync("""
            DECLARE @sql NVARCHAR(MAX) = N'';
            SELECT @sql += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' NOCHECK CONSTRAINT ALL;'
            FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id;
            EXEC sp_executesql @sql;

            SET @sql = N'';
            SELECT @sql += 'DELETE FROM ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ';'
            FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE t.name <> '__EFMigrationsHistory';
            EXEC sp_executesql @sql;

            SET @sql = N'';
            SELECT @sql += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' WITH CHECK CHECK CONSTRAINT ALL;'
            FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id;
            EXEC sp_executesql @sql;
            """);

        // Clear Redis session data
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();
        var server = redis.GetServer(redis.GetEndPoints().First());
        await foreach (var key in server.KeysAsync(pattern: "session:*"))
            await db.KeyDeleteAsync(key);
        await foreach (var key in server.KeysAsync(pattern: "user-sessions:*"))
            await db.KeyDeleteAsync(key);
    }
}

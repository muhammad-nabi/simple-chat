using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace SimpleChat.Infrastructure.IntegrationTests;

[SetUpFixture]
public class TestcontainersFixture
{
    private static MsSqlContainer? _msSqlContainer;
    private static RedisContainer? _redisContainer;

    public static string MsSqlConnectionString => _msSqlContainer?.GetConnectionString()
        ?? throw new InvalidOperationException("MSSQL container not started.");

    public static string RedisConnectionString => _redisContainer?.GetConnectionString()
        ?? throw new InvalidOperationException("Redis container not started.");

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        _redisContainer = new RedisBuilder("redis:7-alpine")
            .Build();

        await Task.WhenAll(
            _msSqlContainer.StartAsync(),
            _redisContainer.StartAsync());
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_msSqlContainer is not null)
            await _msSqlContainer.DisposeAsync();

        if (_redisContainer is not null)
            await _redisContainer.DisposeAsync();
    }
}

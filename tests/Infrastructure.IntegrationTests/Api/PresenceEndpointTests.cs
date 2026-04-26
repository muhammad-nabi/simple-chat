using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using StackExchange.Redis;

namespace SimpleChat.Infrastructure.IntegrationTests.Api;

public class PresenceEndpointTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        await _factory.ResetDatabaseAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Heartbeat_Authenticated_ReturnsOk()
    {
        // Arrange
        string token = await RegisterAndLogin("user1@test.com", "User One");
        SetAuth(token);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/presence/heartbeat",
            new { status = "Online" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Heartbeat_Unauthenticated_Returns401()
    {
        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/presence/heartbeat",
            new { status = "Online" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Heartbeat_InvalidStatus_Returns400()
    {
        // Arrange
        string token = await RegisterAndLogin("user1@test.com", "User One");
        SetAuth(token);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/presence/heartbeat",
            new { status = "Garbage" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Heartbeat_MissingStatus_Returns400()
    {
        // Arrange
        string token = await RegisterAndLogin("user1@test.com", "User One");
        SetAuth(token);

        // Act — empty body
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/presence/heartbeat",
            new { });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Heartbeat_OfflineStatus_Returns400()
    {
        // Arrange
        string token = await RegisterAndLogin("user1@test.com", "User One");
        SetAuth(token);

        // Act — Offline is parseable but rejected by validator
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/presence/heartbeat",
            new { status = "Offline" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetOnlineUsers_NoHeartbeats_ReturnsEmptyList()
    {
        // Arrange
        string token = await RegisterAndLogin("user1@test.com", "User One");
        SetAuth(token);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/presence/online");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetArrayLength(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetOnlineUsers_AfterHeartbeat_ReturnsUser()
    {
        // Arrange
        string token = await RegisterAndLogin("user1@test.com", "User One");
        SetAuth(token);

        // Send heartbeat first
        HttpResponseMessage heartbeatResponse = await _client.PostAsJsonAsync("/api/presence/heartbeat",
            new { status = "Online" });
        heartbeatResponse.EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/presence/online");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetArrayLength(), Is.EqualTo(1));
        Assert.That(body[0].GetProperty("displayName").GetString(), Is.EqualTo("User One"));
        Assert.That(body[0].GetProperty("status").GetString(), Is.EqualTo("Online"));
    }

    [Test]
    public async Task GetOnlineUsers_AwayHeartbeat_ReturnsAwayStatus()
    {
        // Arrange
        string token = await RegisterAndLogin("user1@test.com", "User One");
        SetAuth(token);

        await _client.PostAsJsonAsync("/api/presence/heartbeat",
            new { status = "Away" });

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/presence/online");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetArrayLength(), Is.EqualTo(1));
        Assert.That(body[0].GetProperty("status").GetString(), Is.EqualTo("Away"));
    }

    [Test]
    public async Task GetOnlineUsers_ExpiredHeartbeat_IsPrunedAndReturnedEmpty()
    {
        // Arrange
        string token = await RegisterAndLogin("user1@test.com", "User One");
        SetAuth(token);

        await _client.PostAsJsonAsync("/api/presence/heartbeat",
            new { status = "Online" });

        // Simulate TTL expiry by deleting the presence:{userId} key directly.
        // The userId should still sit in the online_users Set and must be lazy-pruned
        // on the next read per AC#2.
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IConnectionMultiplexer redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
            IDatabase db = redis.GetDatabase();
            IServer server = redis.GetServer(redis.GetEndPoints().First());

            bool deletedAny = false;
            await foreach (RedisKey key in server.KeysAsync(pattern: "presence:*"))
            {
                await db.KeyDeleteAsync(key);
                deletedAny = true;
            }
            Assert.That(deletedAny, Is.True, "Expected a presence:{userId} key to exist after heartbeat.");

            // Confirm online_users Set still references the expired user before the query runs
            long setCount = await db.SetLengthAsync("online_users");
            Assert.That(setCount, Is.EqualTo(1));
        }

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/presence/online");

        // Assert — endpoint returns empty and the Set has been pruned
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetArrayLength(), Is.EqualTo(0));

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IConnectionMultiplexer redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
            IDatabase db = redis.GetDatabase();
            long setCount = await db.SetLengthAsync("online_users");
            Assert.That(setCount, Is.EqualTo(0), "Expired user should have been lazily pruned from online_users.");
        }
    }

    [Test]
    public async Task GetOnlineUsers_Unauthenticated_Returns401()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/presence/online");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // --- Helper Methods ---

    private async Task<string> RegisterAndLogin(string email, string displayName)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register",
            new { displayName, email, password = "password123" });
        response.EnsureSuccessStatusCode();

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private void SetAuth(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

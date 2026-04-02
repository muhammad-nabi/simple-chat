using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace SimpleChat.Infrastructure.IntegrationTests.Hubs;

public class ChatHubTests
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
    public async Task Connect_WithValidJwt_ShouldSucceed()
    {
        // Arrange
        string token = await RegisterAndLogin("user1@test.com", "User One");

        HubConnection connection = CreateHubConnection(token);

        // Act
        await connection.StartAsync();

        // Assert
        connection.State.ShouldBe(HubConnectionState.Connected);

        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Test]
    public async Task Connect_WithoutJwt_ShouldFail()
    {
        // Arrange
        HubConnection connection = CreateHubConnection(accessToken: null);

        // Act & Assert
        Should.Throw<Exception>(async () => await connection.StartAsync());

        await connection.DisposeAsync();
    }

    [Test]
    public async Task SendMessage_ViaHub_ShouldPersistToDatabase()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        SetAuth(token1);

        long conversationId = await CreateConversation(user2Id);

        HubConnection connection = CreateHubConnection(token1);
        await connection.StartAsync();

        // Act
        long messageId = await connection.InvokeAsync<long>("SendMessage", conversationId, "Hello from hub!");

        // Assert
        messageId.ShouldBeGreaterThan(0);

        // Verify message is in database via REST API
        HttpResponseMessage response = await _client.GetAsync(
            $"/api/conversations/{conversationId}/messages");
        response.EnsureSuccessStatusCode();
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement messages = body.GetProperty("messages");
        messages.GetArrayLength().ShouldBe(1);
        messages[0].GetProperty("content").GetString().ShouldBe("Hello from hub!");

        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Test]
    public async Task SendMessage_ViaHub_ShouldBroadcastToGroupMembers()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        (string token2, string user2Id) = await RegisterAndLoginWithId("user2@test.com", "User Two");
        SetAuth(token1);

        long conversationId = await CreateConversation(user2Id);

        HubConnection sender = CreateHubConnection(token1);
        HubConnection receiver = CreateHubConnection(token2);

        TaskCompletionSource<JsonElement> receivedMessage = new();
        receiver.On<JsonElement>("ReceiveMessage", msg =>
        {
            receivedMessage.TrySetResult(msg);
        });

        await sender.StartAsync();
        await receiver.StartAsync();

        // Act
        await sender.InvokeAsync<long>("SendMessage", conversationId, "Real-time message!");

        // Assert — receiver should get the broadcast
        JsonElement message = await receivedMessage.Task.WaitAsync(TimeSpan.FromSeconds(5));
        message.GetProperty("content").GetString().ShouldBe("Real-time message!");
        message.GetProperty("conversationId").GetInt64().ShouldBe(conversationId);
        message.GetProperty("senderDisplayName").GetString().ShouldBe("User One");

        await sender.StopAsync();
        await receiver.StopAsync();
        await sender.DisposeAsync();
        await receiver.DisposeAsync();
    }

    // --- Helper Methods ---

    private HubConnection CreateHubConnection(string? accessToken)
    {
        HubConnectionBuilder builder = new();
        builder.Services.AddSingleton(_factory.Server);

        return builder
            .WithUrl($"{_factory.Server.BaseAddress}hubs/chat", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                if (accessToken != null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                }
            })
            .Build();
    }

    private async Task<string> RegisterAndLogin(string email, string displayName)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register",
            new { displayName, email, password = "password123" });
        response.EnsureSuccessStatusCode();

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> RegisterAndGetUserId(string email, string displayName)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register",
            new { displayName, email, password = "password123" });
        response.EnsureSuccessStatusCode();

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("userId").GetString()!;
    }

    private async Task<(string Token, string UserId)> RegisterAndLoginWithId(string email, string displayName)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register",
            new { displayName, email, password = "password123" });
        response.EnsureSuccessStatusCode();

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("accessToken").GetString()!, body.GetProperty("userId").GetString()!);
    }

    private void SetAuth(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<long> CreateConversation(string otherUserId)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/conversations",
            new { otherUserId });
        response.EnsureSuccessStatusCode();

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt64();
    }
}

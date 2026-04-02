using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

namespace SimpleChat.Infrastructure.IntegrationTests.Api;

public class ConversationEndpointTests
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

    // --- Create Conversation Tests ---

    [Test]
    public async Task CreateConversation_WithValidData_ReturnsOkWithId()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string token2UserId = await RegisterAndGetUserId("user2@test.com", "User Two");
        SetAuth(token1);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/conversations",
            new { otherUserId = token2UserId });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("id").GetInt64(), Is.GreaterThan(0));
    }

    [Test]
    public async Task CreateConversation_DuplicateUsers_ReturnsExistingConversation()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        SetAuth(token1);

        // Act - create twice
        HttpResponseMessage response1 = await _client.PostAsJsonAsync("/api/conversations",
            new { otherUserId = user2Id });
        HttpResponseMessage response2 = await _client.PostAsJsonAsync("/api/conversations",
            new { otherUserId = user2Id });

        // Assert
        JsonElement body1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement body2 = await response2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body1.GetProperty("id").GetInt64(), Is.EqualTo(body2.GetProperty("id").GetInt64()));
    }

    // --- Send Message Tests ---

    [Test]
    public async Task SendMessage_ValidMessage_ReturnsOkWithId()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        SetAuth(token1);

        long conversationId = await CreateConversation(user2Id);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/conversations/{conversationId}/messages",
            new { content = "Hello!" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("id").GetInt64(), Is.GreaterThan(0));
    }

    // --- Get Message History Tests ---

    [Test]
    public async Task GetMessageHistory_ReturnsMessages()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        SetAuth(token1);

        long conversationId = await CreateConversation(user2Id);
        await SendMessage(conversationId, "First message");
        await SendMessage(conversationId, "Second message");

        // Act
        HttpResponseMessage response = await _client.GetAsync(
            $"/api/conversations/{conversationId}/messages");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("messages").GetArrayLength(), Is.EqualTo(2));
        Assert.That(body.GetProperty("hasMore").GetBoolean(), Is.False);
    }

    [Test]
    public async Task GetMessageHistory_CursorPagination_ReturnsCorrectPage()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        SetAuth(token1);

        long conversationId = await CreateConversation(user2Id);
        await SendMessage(conversationId, "Message 1");
        await SendMessage(conversationId, "Message 2");
        await SendMessage(conversationId, "Message 3");

        // Get first page (limit 2)
        HttpResponseMessage response1 = await _client.GetAsync(
            $"/api/conversations/{conversationId}/messages?limit=2");
        JsonElement body1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body1.GetProperty("hasMore").GetBoolean(), Is.True);

        long nextCursor = body1.GetProperty("nextCursor").GetInt64();

        // Act - get second page
        HttpResponseMessage response2 = await _client.GetAsync(
            $"/api/conversations/{conversationId}/messages?before={nextCursor}&limit=2");

        // Assert
        JsonElement body2 = await response2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body2.GetProperty("messages").GetArrayLength(), Is.EqualTo(1));
        Assert.That(body2.GetProperty("hasMore").GetBoolean(), Is.False);
    }

    // --- Auth Tests ---

    [Test]
    public async Task CreateConversation_Unauthenticated_Returns401()
    {
        // Act (no auth header)
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/conversations",
            new { otherUserId = "some-id" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task SendMessage_NonParticipant_Returns403()
    {
        // Arrange - user1 creates conversation with user2
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string token3 = await RegisterAndLogin("user3@test.com", "User Three");
        SetAuth(token1);

        long conversationId = await CreateConversation(user2Id);

        // Act - user3 tries to send message
        SetAuth(token3);
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/conversations/{conversationId}/messages",
            new { content = "I shouldn't be here" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetMessageHistory_NonParticipant_Returns403()
    {
        // Arrange - user1 creates conversation with user2
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string token3 = await RegisterAndLogin("user3@test.com", "User Three");
        SetAuth(token1);

        long conversationId = await CreateConversation(user2Id);

        // Act - user3 tries to read messages
        SetAuth(token3);
        HttpResponseMessage response = await _client.GetAsync(
            $"/api/conversations/{conversationId}/messages");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task SendMessage_InvalidContent_Returns400()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        SetAuth(token1);

        long conversationId = await CreateConversation(user2Id);

        // Act - empty content
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/conversations/{conversationId}/messages",
            new { content = "" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
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

    private async Task<string> RegisterAndGetUserId(string email, string displayName)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register",
            new { displayName, email, password = "password123" });
        response.EnsureSuccessStatusCode();

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("userId").GetString()!;
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

    private async Task<long> SendMessage(long conversationId, string content)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/conversations/{conversationId}/messages",
            new { content });
        response.EnsureSuccessStatusCode();

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt64();
    }
}

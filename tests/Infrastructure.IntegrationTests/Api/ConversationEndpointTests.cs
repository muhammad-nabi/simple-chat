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

    // --- Get Conversations Tests ---

    [Test]
    public async Task GetConversations_Authenticated_ReturnsOk()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        SetAuth(token1);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/conversations");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetArrayLength(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetConversations_WithConversations_ReturnsConversationsWithDetails()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        SetAuth(token1);

        long conversationId = await CreateConversation(user2Id);
        await SendMessage(conversationId, "Hello!");

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/conversations");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetArrayLength(), Is.EqualTo(1));

        JsonElement conv = body[0];
        Assert.That(conv.GetProperty("id").GetInt64(), Is.EqualTo(conversationId));
        Assert.That(conv.GetProperty("type").GetString(), Is.EqualTo("Private"));
        Assert.That(conv.GetProperty("lastMessagePreview").GetString(), Is.EqualTo("Hello!"));
        Assert.That(conv.GetProperty("otherParticipants").GetArrayLength(), Is.EqualTo(1));
        Assert.That(conv.GetProperty("otherParticipants")[0].GetProperty("displayName").GetString(), Is.EqualTo("User Two"));
    }

    [Test]
    public async Task GetConversations_SortedByLastMessageAt()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user3Id = await RegisterAndGetUserId("user3@test.com", "User Three");
        SetAuth(token1);

        long conv1 = await CreateConversation(user2Id);
        await SendMessage(conv1, "Old message");
        long conv2 = await CreateConversation(user3Id);
        await SendMessage(conv2, "New message");

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/conversations");

        // Assert
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetArrayLength(), Is.EqualTo(2));
        Assert.That(body[0].GetProperty("id").GetInt64(), Is.EqualTo(conv2)); // Most recent first
        Assert.That(body[1].GetProperty("id").GetInt64(), Is.EqualTo(conv1));
    }

    [Test]
    public async Task GetConversations_Unauthenticated_Returns401()
    {
        // Act (no auth header)
        HttpResponseMessage response = await _client.GetAsync("/api/conversations");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // --- Group Conversation Tests ---

    [Test]
    public async Task CreateGroupConversation_WithValidData_ReturnsOkWithId()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user3Id = await RegisterAndGetUserId("user3@test.com", "User Three");
        SetAuth(token1);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user3Id }, groupName = "Engineering Team" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("id").GetInt64(), Is.GreaterThan(0));
    }

    [Test]
    public async Task CreateGroupConversation_AppearsInConversationList()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user3Id = await RegisterAndGetUserId("user3@test.com", "User Three");
        SetAuth(token1);

        await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user3Id }, groupName = "Engineering Team" });

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/conversations");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetArrayLength(), Is.EqualTo(1));

        JsonElement conv = body[0];
        Assert.That(conv.GetProperty("type").GetString(), Is.EqualTo("Group"));
        Assert.That(conv.GetProperty("name").GetString(), Is.EqualTo("Engineering Team"));
        Assert.That(conv.GetProperty("otherParticipants").GetArrayLength(), Is.EqualTo(2));
    }

    [Test]
    public async Task CreateGroupConversation_VisibleToAllParticipants()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string token3 = await RegisterAndLogin("user3@test.com", "User Three");

        // User 2 needs a token too
        HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "user2@test.com", password = "password123" });
        loginResponse.EnsureSuccessStatusCode();
        JsonElement loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        string token2 = loginBody.GetProperty("accessToken").GetString()!;

        // Get user3 ID
        SetAuth(token3);
        HttpResponseMessage user3Response = await _client.GetAsync("/api/conversations");
        user3Response.EnsureSuccessStatusCode();

        // Retrieve user3 ID from the registration step — we need to extract it differently
        // User3 was registered via RegisterAndLogin, get their userId
        SetAuth(token1);

        // Create group (user1 creates with user2 and user3)
        // First get user3's ID by registering fresh
        string user3Id = await GetUserIdFromLogin("user3@test.com", "password123");

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user3Id }, groupName = "Team Chat" });
        createResponse.EnsureSuccessStatusCode();

        // Act — check user2 can see the group
        SetAuth(token2);
        HttpResponseMessage response2 = await _client.GetAsync("/api/conversations");
        JsonElement body2 = await response2.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.That(body2.GetArrayLength(), Is.EqualTo(1));
        Assert.That(body2[0].GetProperty("name").GetString(), Is.EqualTo("Team Chat"));
    }

    [Test]
    public async Task CreateGroupConversation_NoGroupName_Returns400()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user3Id = await RegisterAndGetUserId("user3@test.com", "User Three");
        SetAuth(token1);

        // Act — missing groupName
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user3Id } });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GroupConversation_HasSystemMessageInHistory()
    {
        // Arrange — create group conversation
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user3Id = await RegisterAndGetUserId("user3@test.com", "User Three");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user3Id }, groupName = "Dev Team" });
        createResponse.EnsureSuccessStatusCode();
        JsonElement createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        long conversationId = createBody.GetProperty("id").GetInt64();

        // Act — get message history (should contain system message from creation)
        HttpResponseMessage historyResponse = await _client.GetAsync(
            $"/api/conversations/{conversationId}/messages");
        historyResponse.EnsureSuccessStatusCode();
        JsonElement historyBody = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert — system message exists
        JsonElement messages = historyBody.GetProperty("messages");
        Assert.That(messages.GetArrayLength(), Is.EqualTo(1));
        Assert.That(messages[0].GetProperty("messageType").GetString(), Is.EqualTo("System"));
        Assert.That(messages[0].GetProperty("content").GetString(), Does.Contain("created the group"));
    }

    [Test]
    public async Task GroupConversation_SendAndRetrieveMessages()
    {
        // Arrange — create group and send messages
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user3Id = await RegisterAndGetUserId("user3@test.com", "User Three");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user3Id }, groupName = "Dev Team" });
        createResponse.EnsureSuccessStatusCode();
        JsonElement createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        long conversationId = createBody.GetProperty("id").GetInt64();

        // Act — send a text message and retrieve history
        await SendMessage(conversationId, "Hello team!");
        HttpResponseMessage historyResponse = await _client.GetAsync(
            $"/api/conversations/{conversationId}/messages");
        historyResponse.EnsureSuccessStatusCode();
        JsonElement historyBody = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert — system message + text message
        JsonElement messages = historyBody.GetProperty("messages");
        Assert.That(messages.GetArrayLength(), Is.EqualTo(2));

        // Messages are in descending order (newest first)
        Assert.That(messages[0].GetProperty("messageType").GetString(), Is.EqualTo("Text"));
        Assert.That(messages[0].GetProperty("content").GetString(), Is.EqualTo("Hello team!"));
        Assert.That(messages[0].GetProperty("senderDisplayName").GetString(), Is.EqualTo("User One"));

        Assert.That(messages[1].GetProperty("messageType").GetString(), Is.EqualTo("System"));
    }

    [Test]
    public async Task PrivateConversation_NoSystemMessageCreated()
    {
        // Arrange — create private conversation
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        SetAuth(token1);

        long conversationId = await CreateConversation(user2Id);

        // Act — get message history
        HttpResponseMessage historyResponse = await _client.GetAsync(
            $"/api/conversations/{conversationId}/messages");
        historyResponse.EnsureSuccessStatusCode();
        JsonElement historyBody = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert — no system message for private conversations
        JsonElement messages = historyBody.GetProperty("messages");
        Assert.That(messages.GetArrayLength(), Is.EqualTo(0));
    }

    // --- Browse Groups Tests ---

    [Test]
    public async Task BrowseGroups_ReturnsGroupsUserIsNotIn()
    {
        // Arrange — user1 creates a group with user2 and user4, user3 is NOT in it
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string token3 = await RegisterAndLogin("user3@test.com", "User Three");
        string user4Id = await RegisterAndGetUserId("user4@test.com", "User Four");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user4Id }, groupName = "Alpha Team" });
        createResponse.EnsureSuccessStatusCode();

        // Act — user3 browses groups
        SetAuth(token3);
        HttpResponseMessage response = await _client.GetAsync("/api/conversations/browse");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetArrayLength(), Is.EqualTo(1));
        Assert.That(body[0].GetProperty("name").GetString(), Is.EqualTo("Alpha Team"));
        Assert.That(body[0].GetProperty("participantCount").GetInt32(), Is.EqualTo(3));
    }

    [Test]
    public async Task BrowseGroups_ExcludesGroupsUserIsIn()
    {
        // Arrange — user1 creates a group with user2 and user3; user1 should NOT see it in browse
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user3Id = await RegisterAndGetUserId("user3@test.com", "User Three");
        SetAuth(token1);

        await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user3Id }, groupName = "My Group" });

        // Act — user1 browses (should see empty list since they're in the only group)
        HttpResponseMessage response = await _client.GetAsync("/api/conversations/browse");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetArrayLength(), Is.EqualTo(0));
    }

    [Test]
    public async Task BrowseGroups_Unauthenticated_Returns401()
    {
        // Act (no auth header)
        HttpResponseMessage response = await _client.GetAsync("/api/conversations/browse");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // --- Join Group Tests ---

    [Test]
    public async Task JoinGroup_AddsUserAndCreatesSystemMessage()
    {
        // Arrange — user1 creates a group with user2 and user4, user3 joins
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string token3 = await RegisterAndLogin("user3@test.com", "User Three");
        string user4Id = await RegisterAndGetUserId("user4@test.com", "User Four");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user4Id }, groupName = "Open Group" });
        createResponse.EnsureSuccessStatusCode();
        JsonElement createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        long groupId = createBody.GetProperty("id").GetInt64();

        // Act — user3 joins the group
        SetAuth(token3);
        HttpResponseMessage joinResponse = await _client.PostAsync(
            $"/api/conversations/{groupId}/join", null);

        // Assert — join succeeds
        Assert.That(joinResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Assert — group now appears in user3's conversation list
        HttpResponseMessage convResponse = await _client.GetAsync("/api/conversations");
        JsonElement convBody = await convResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(convBody.GetArrayLength(), Is.EqualTo(1));
        Assert.That(convBody[0].GetProperty("name").GetString(), Is.EqualTo("Open Group"));

        // Assert — system message exists in message history
        HttpResponseMessage historyResponse = await _client.GetAsync(
            $"/api/conversations/{groupId}/messages");
        JsonElement historyBody = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement messages = historyBody.GetProperty("messages");

        // Should have 2 messages: "created the group" + "joined the group"
        Assert.That(messages.GetArrayLength(), Is.EqualTo(2));
        Assert.That(messages[0].GetProperty("content").GetString(), Does.Contain("joined the group"));
        Assert.That(messages[0].GetProperty("messageType").GetString(), Is.EqualTo("System"));
    }

    [Test]
    public async Task JoinGroup_GroupNoLongerInBrowseList()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string token3 = await RegisterAndLogin("user3@test.com", "User Three");
        string user4Id = await RegisterAndGetUserId("user4@test.com", "User Four");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user4Id }, groupName = "Team" });
        JsonElement createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        long groupId = createBody.GetProperty("id").GetInt64();

        // User3 joins
        SetAuth(token3);
        await _client.PostAsync($"/api/conversations/{groupId}/join", null);

        // Act — user3 browses groups again
        HttpResponseMessage browseResponse = await _client.GetAsync("/api/conversations/browse");
        JsonElement browseBody = await browseResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert — joined group no longer appears
        Assert.That(browseBody.GetArrayLength(), Is.EqualTo(0));
    }

    [Test]
    public async Task JoinGroup_AlreadyMember_Returns403()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user3Id = await RegisterAndGetUserId("user3@test.com", "User Three");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user3Id }, groupName = "My Group" });
        JsonElement createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        long groupId = createBody.GetProperty("id").GetInt64();

        // Act — user1 tries to join a group they're already in
        HttpResponseMessage joinResponse = await _client.PostAsync(
            $"/api/conversations/{groupId}/join", null);

        // Assert
        Assert.That(joinResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task JoinGroup_PrivateConversation_Returns403()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string token3 = await RegisterAndLogin("user3@test.com", "User Three");
        SetAuth(token1);

        long privateConvId = await CreateConversation(user2Id);

        // Act — user3 tries to join a private conversation
        SetAuth(token3);
        HttpResponseMessage joinResponse = await _client.PostAsync(
            $"/api/conversations/{privateConvId}/join", null);

        // Assert
        Assert.That(joinResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task JoinGroup_NonExistentConversation_Returns404()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        SetAuth(token1);

        // Act
        HttpResponseMessage joinResponse = await _client.PostAsync(
            "/api/conversations/99999/join", null);

        // Assert
        Assert.That(joinResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task JoinGroup_Unauthenticated_Returns401()
    {
        // Act (no auth header)
        HttpResponseMessage response = await _client.PostAsync(
            "/api/conversations/1/join", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
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

    // --- Get Group Members Tests ---

    [Test]
    public async Task GetGroupMembers_ReturnsAllMembers()
    {
        // Arrange — user1 creates a group with user2 and user3
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user3Id = await RegisterAndGetUserId("user3@test.com", "User Three");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user3Id }, groupName = "Team" });
        JsonElement createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        long groupId = createBody.GetProperty("id").GetInt64();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/conversations/{groupId}/members");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        JsonElement members = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(members.GetArrayLength(), Is.EqualTo(3));
    }

    [Test]
    public async Task GetGroupMembers_NonParticipant_Returns403()
    {
        // Arrange — user1 creates a group with user2 and user4, user3 is not in it
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string token3 = await RegisterAndLogin("user3@test.com", "User Three");
        string user4Id = await RegisterAndGetUserId("user4@test.com", "User Four");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user4Id }, groupName = "Private Team" });
        JsonElement createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        long groupId = createBody.GetProperty("id").GetInt64();

        // Act — user3 tries to get members
        SetAuth(token3);
        HttpResponseMessage response = await _client.GetAsync($"/api/conversations/{groupId}/members");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetGroupMembers_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        HttpResponseMessage response = await _client.GetAsync("/api/conversations/1/members");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // --- Invite to Group Tests ---

    [Test]
    public async Task InviteToGroup_AddsUsersAndCreatesSystemMessages()
    {
        // Arrange — user1 creates a group with user2 and user4, then invites user3
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user3Id = await RegisterAndGetUserId("user3@test.com", "User Three");
        string user4Id = await RegisterAndGetUserId("user4@test.com", "User Four");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user4Id }, groupName = "Team" });
        JsonElement createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        long groupId = createBody.GetProperty("id").GetInt64();

        // Act — invite user3
        HttpResponseMessage inviteResponse = await _client.PostAsJsonAsync(
            $"/api/conversations/{groupId}/invite",
            new { userIds = new[] { user3Id } });

        // Assert — invite succeeds
        Assert.That(inviteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify user3 appears in members (user1 + user2 + user4 + user3 = 4)
        HttpResponseMessage membersResponse = await _client.GetAsync($"/api/conversations/{groupId}/members");
        JsonElement members = await membersResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(members.GetArrayLength(), Is.EqualTo(4));

        // Verify system message was created
        HttpResponseMessage historyResponse = await _client.GetAsync($"/api/conversations/{groupId}/messages");
        JsonElement historyBody = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement messages = historyBody.GetProperty("messages");
        bool hasAddedMessage = false;
        foreach (JsonElement msg in messages.EnumerateArray())
        {
            if (msg.GetProperty("content").GetString()?.Contains("added") == true)
            {
                hasAddedMessage = true;
                break;
            }
        }
        Assert.That(hasAddedMessage, Is.True);
    }

    [Test]
    public async Task InviteToGroup_PrivateConversation_Returns403()
    {
        // Arrange — create private conversation
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user3Id = await RegisterAndGetUserId("user3@test.com", "User Three");
        SetAuth(token1);

        long privateConvId = await CreateConversation(user2Id);

        // Act — try to invite user3 to private conversation
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/conversations/{privateConvId}/invite",
            new { userIds = new[] { user3Id } });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task InviteToGroup_NonParticipant_Returns403()
    {
        // Arrange — user1 creates a group with user2 and user5, user3 tries to invite user4
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string token3 = await RegisterAndLogin("user3@test.com", "User Three");
        string user4Id = await RegisterAndGetUserId("user4@test.com", "User Four");
        string user5Id = await RegisterAndGetUserId("user5@test.com", "User Five");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user5Id }, groupName = "Team" });
        JsonElement createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        long groupId = createBody.GetProperty("id").GetInt64();

        // Act — user3 (not in group) tries to invite user4
        SetAuth(token3);
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/conversations/{groupId}/invite",
            new { userIds = new[] { user4Id } });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task InviteToGroup_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/conversations/1/invite",
            new { userIds = new[] { "user-1" } });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // --- Leave Group Tests ---

    [Test]
    public async Task LeaveGroup_RemovesUserAndCreatesSystemMessage()
    {
        // Arrange — user1 creates a group with user2 and user3, user2 leaves
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user3Id = await RegisterAndGetUserId("user3@test.com", "User Three");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user3Id }, groupName = "Team" });
        JsonElement createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        long groupId = createBody.GetProperty("id").GetInt64();

        // Login as user2 to leave
        HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "user2@test.com", password = "password123" });
        JsonElement loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        string token2Actual = loginBody.GetProperty("accessToken").GetString()!;

        // Act — user2 leaves the group
        SetAuth(token2Actual);
        HttpResponseMessage leaveResponse = await _client.PostAsync(
            $"/api/conversations/{groupId}/leave", null);

        // Assert — leave succeeds
        Assert.That(leaveResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify user2 no longer in members (check from user1's perspective)
        SetAuth(token1);
        HttpResponseMessage membersResponse = await _client.GetAsync($"/api/conversations/{groupId}/members");
        JsonElement members = await membersResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(members.GetArrayLength(), Is.EqualTo(2)); // user1 + user3

        // Verify system message was created
        HttpResponseMessage historyResponse = await _client.GetAsync($"/api/conversations/{groupId}/messages");
        JsonElement historyBody = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement messages = historyBody.GetProperty("messages");
        bool hasLeftMessage = false;
        foreach (JsonElement msg in messages.EnumerateArray())
        {
            if (msg.GetProperty("content").GetString()?.Contains("left the group") == true)
            {
                hasLeftMessage = true;
                break;
            }
        }
        Assert.That(hasLeftMessage, Is.True);
    }

    [Test]
    public async Task LeaveGroup_LastParticipant_PreservesConversation()
    {
        // Arrange — user1 creates a group with user2 and user4
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string user4Id = await RegisterAndGetUserId("user4@test.com", "User Four");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user4Id }, groupName = "Temp Team" });
        JsonElement createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        long groupId = createBody.GetProperty("id").GetInt64();

        // User2 leaves
        HttpResponseMessage loginResponse2 = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "user2@test.com", password = "password123" });
        JsonElement loginBody2 = await loginResponse2.Content.ReadFromJsonAsync<JsonElement>();
        SetAuth(loginBody2.GetProperty("accessToken").GetString()!);
        await _client.PostAsync($"/api/conversations/{groupId}/leave", null);

        // User4 leaves
        HttpResponseMessage loginResponse4 = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "user4@test.com", password = "password123" });
        JsonElement loginBody4 = await loginResponse4.Content.ReadFromJsonAsync<JsonElement>();
        SetAuth(loginBody4.GetProperty("accessToken").GetString()!);
        await _client.PostAsync($"/api/conversations/{groupId}/leave", null);

        // User1 leaves (last participant)
        SetAuth(token1);
        HttpResponseMessage leaveResponse = await _client.PostAsync(
            $"/api/conversations/{groupId}/leave", null);
        Assert.That(leaveResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Act — register new user and check browse groups — the empty group should still exist
        string token3 = await RegisterAndLogin("user3@test.com", "User Three");
        SetAuth(token3);
        HttpResponseMessage browseResponse = await _client.GetAsync("/api/conversations/browse");
        JsonElement browseBody = await browseResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert — the group should still exist in browse
        Assert.That(browseBody.GetArrayLength(), Is.GreaterThanOrEqualTo(1));
        bool foundGroup = false;
        foreach (JsonElement group in browseBody.EnumerateArray())
        {
            if (group.GetProperty("name").GetString() == "Temp Team")
            {
                foundGroup = true;
                break;
            }
        }
        Assert.That(foundGroup, Is.True);
    }

    [Test]
    public async Task LeaveGroup_PrivateConversation_Returns403()
    {
        // Arrange
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        SetAuth(token1);

        long privateConvId = await CreateConversation(user2Id);

        // Act — try to leave private conversation
        HttpResponseMessage response = await _client.PostAsync(
            $"/api/conversations/{privateConvId}/leave", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task LeaveGroup_NonParticipant_Returns403()
    {
        // Arrange — user1 creates a group with user2 and user4, user3 tries to leave
        string token1 = await RegisterAndLogin("user1@test.com", "User One");
        string user2Id = await RegisterAndGetUserId("user2@test.com", "User Two");
        string token3 = await RegisterAndLogin("user3@test.com", "User Three");
        string user4Id = await RegisterAndGetUserId("user4@test.com", "User Four");
        SetAuth(token1);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/conversations",
            new { participantIds = new[] { user2Id, user4Id }, groupName = "Team" });
        JsonElement createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        long groupId = createBody.GetProperty("id").GetInt64();

        // Act — user3 (not in group) tries to leave
        SetAuth(token3);
        HttpResponseMessage response = await _client.PostAsync(
            $"/api/conversations/{groupId}/leave", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task LeaveGroup_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        HttpResponseMessage response = await _client.PostAsync(
            "/api/conversations/1/leave", null);
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

    private async Task<string> GetUserIdFromLogin(string email, string password)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email, password });
        response.EnsureSuccessStatusCode();

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("userId").GetString()!;
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

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;
using SimpleChat.Infrastructure.Data;
using SimpleChat.Infrastructure.Identity;

namespace SimpleChat.Infrastructure.IntegrationTests.Data;

[TestFixture]
public class MessagingEntityTests
{
    private CustomWebApplicationFactory _factory = null!;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new CustomWebApplicationFactory();
        await _factory.ResetDatabaseAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task Conversation_CreateAndReadBack_ShouldRoundTripAllProperties()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        string userId = await CreateTestUser(scope, "user1@test.com", "User One");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Conversation conversation = new()
        {
            Type = ConversationType.Private,
            Name = null,
            CreatedById = userId,
            LastMessageAt = now
        };

        // Act
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync(CancellationToken.None);

        // Assert — read back from fresh context to verify persistence
        using IServiceScope readScope = _factory.Services.CreateScope();
        ApplicationDbContext readContext = readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Conversation? loaded = await readContext.Conversations.FindAsync(conversation.Id);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Id, Is.GreaterThan(0));
        Assert.That(loaded.Type, Is.EqualTo(ConversationType.Private));
        Assert.That(loaded.Name, Is.Null);
        Assert.That(loaded.CreatedById, Is.EqualTo(userId));
        Assert.That(loaded.LastMessageAt, Is.Not.Null);
        Assert.That(loaded.CreatedAt, Is.Not.EqualTo(default(DateTimeOffset)));
    }

    [Test]
    public async Task ConversationParticipant_AddParticipants_ShouldPersistWithCompositeKey()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        string user1Id = await CreateTestUser(scope, "user1@test.com", "User One");
        string user2Id = await CreateTestUser(scope, "user2@test.com", "User Two");

        Conversation conversation = new()
        {
            Type = ConversationType.Private,
            CreatedById = user1Id
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync(CancellationToken.None);

        DateTimeOffset joinedAt = DateTimeOffset.UtcNow;
        context.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = user1Id,
            JoinedAt = joinedAt
        });
        context.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = user2Id,
            JoinedAt = joinedAt
        });
        await context.SaveChangesAsync(CancellationToken.None);

        // Act — read back
        using IServiceScope readScope = _factory.Services.CreateScope();
        ApplicationDbContext readContext = readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        List<ConversationParticipant> participants = await readContext.ConversationParticipants
            .Where(cp => cp.ConversationId == conversation.Id)
            .ToListAsync();

        // Assert
        Assert.That(participants, Has.Count.EqualTo(2));
        Assert.That(participants.Select(cp => cp.UserId), Does.Contain(user1Id));
        Assert.That(participants.Select(cp => cp.UserId), Does.Contain(user2Id));
    }

    [Test]
    public async Task ConversationParticipant_DuplicateCompositeKey_ShouldThrowDbUpdateException()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        string userId = await CreateTestUser(scope, "user1@test.com", "User One");

        Conversation conversation = new()
        {
            Type = ConversationType.Private,
            CreatedById = userId
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync(CancellationToken.None);

        context.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = userId,
            JoinedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync(CancellationToken.None);

        // Act & Assert — duplicate via fresh context should hit DB constraint
        using IServiceScope dupScope = _factory.Services.CreateScope();
        ApplicationDbContext dupContext = dupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dupContext.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = userId,
            JoinedAt = DateTimeOffset.UtcNow
        });

        Assert.ThrowsAsync<DbUpdateException>(
            () => dupContext.SaveChangesAsync(CancellationToken.None));
    }

    [Test]
    public async Task Message_CreateAndQueryWithCursorPagination_ShouldReturnCorrectOrder()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        string userId = await CreateTestUser(scope, "user1@test.com", "User One");

        Conversation conversation = new()
        {
            Type = ConversationType.Private,
            CreatedById = userId
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync(CancellationToken.None);

        // Create 5 messages
        for (int i = 1; i <= 5; i++)
        {
            context.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                SenderId = userId,
                Content = $"Message {i}",
                SentAt = DateTimeOffset.UtcNow.AddMinutes(i),
                MessageType = MessageType.Text
            });
        }
        await context.SaveChangesAsync(CancellationToken.None);

        // Act — cursor-based pagination: get messages before the last one
        using IServiceScope readScope = _factory.Services.CreateScope();
        ApplicationDbContext readContext = readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        List<Message> allMessages = await readContext.Messages
            .Where(m => m.ConversationId == conversation.Id)
            .OrderByDescending(m => m.Id)
            .ToListAsync();

        long cursor = allMessages[0].Id; // newest message Id
        List<Message> olderMessages = await readContext.Messages
            .Where(m => m.ConversationId == conversation.Id && m.Id < cursor)
            .OrderByDescending(m => m.Id)
            .Take(3)
            .ToListAsync();

        // Assert
        Assert.That(allMessages, Has.Count.EqualTo(5));
        Assert.That(olderMessages, Has.Count.EqualTo(3));
        // Verify descending order
        Assert.That(olderMessages[0].Id, Is.GreaterThan(olderMessages[1].Id));
        Assert.That(olderMessages[1].Id, Is.GreaterThan(olderMessages[2].Id));
        // All should be less than cursor
        Assert.That(olderMessages, Has.All.Matches<Message>(m => m.Id < cursor));
    }

    [Test]
    public async Task Conversation_CascadeDelete_ShouldRemoveParticipantsAndMessages()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        string userId = await CreateTestUser(scope, "user1@test.com", "User One");

        Conversation conversation = new()
        {
            Type = ConversationType.Private,
            CreatedById = userId
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync(CancellationToken.None);

        context.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = userId,
            JoinedAt = DateTimeOffset.UtcNow
        });
        context.Messages.Add(new Message
        {
            ConversationId = conversation.Id,
            SenderId = userId,
            Content = "Hello",
            SentAt = DateTimeOffset.UtcNow,
            MessageType = MessageType.Text
        });
        await context.SaveChangesAsync(CancellationToken.None);

        long conversationId = conversation.Id;

        // Act — delete conversation
        context.Conversations.Remove(conversation);
        await context.SaveChangesAsync(CancellationToken.None);

        // Assert — participants and messages should be cascade-deleted
        using IServiceScope readScope = _factory.Services.CreateScope();
        ApplicationDbContext readContext = readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        List<ConversationParticipant> participants = await readContext.ConversationParticipants
            .Where(cp => cp.ConversationId == conversationId)
            .ToListAsync();
        List<Message> messages = await readContext.Messages
            .Where(m => m.ConversationId == conversationId)
            .ToListAsync();

        Assert.That(participants, Is.Empty);
        Assert.That(messages, Is.Empty);
    }

    [Test]
    public async Task ConversationParticipant_LastReadMessageId_ShouldBeNullableAndSettable()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        string userId = await CreateTestUser(scope, "user1@test.com", "User One");

        Conversation conversation = new()
        {
            Type = ConversationType.Private,
            CreatedById = userId
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync(CancellationToken.None);

        // Create a message to use as LastReadMessageId
        Message message = new()
        {
            ConversationId = conversation.Id,
            SenderId = userId,
            Content = "Hello",
            SentAt = DateTimeOffset.UtcNow,
            MessageType = MessageType.Text
        };
        context.Messages.Add(message);
        await context.SaveChangesAsync(CancellationToken.None);

        // Add participant with null LastReadMessageId
        ConversationParticipant participant = new()
        {
            ConversationId = conversation.Id,
            UserId = userId,
            JoinedAt = DateTimeOffset.UtcNow,
            LastReadMessageId = null
        };
        context.ConversationParticipants.Add(participant);
        await context.SaveChangesAsync(CancellationToken.None);

        // Assert — null value persists
        using IServiceScope readScope1 = _factory.Services.CreateScope();
        ApplicationDbContext readContext1 = readScope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ConversationParticipant? loaded1 = await readContext1.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversation.Id && cp.UserId == userId);
        Assert.That(loaded1, Is.Not.Null);
        Assert.That(loaded1!.LastReadMessageId, Is.Null);

        // Act — update to valid message Id
        using IServiceScope updateScope = _factory.Services.CreateScope();
        ApplicationDbContext updateContext = updateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ConversationParticipant? toUpdate = await updateContext.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversation.Id && cp.UserId == userId);
        Assert.That(toUpdate, Is.Not.Null);
        toUpdate!.LastReadMessageId = message.Id;
        await updateContext.SaveChangesAsync(CancellationToken.None);

        // Assert — updated value persists
        using IServiceScope readScope2 = _factory.Services.CreateScope();
        ApplicationDbContext readContext2 = readScope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ConversationParticipant? loaded2 = await readContext2.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversation.Id && cp.UserId == userId);
        Assert.That(loaded2, Is.Not.Null);
        Assert.That(loaded2!.LastReadMessageId, Is.EqualTo(message.Id));
    }

    private static async Task<string> CreateTestUser(IServiceScope scope, string email, string displayName)
    {
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
            DisplayName = displayName
        };
        IdentityResult result = await userManager.CreateAsync(user, "TestPass123!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
        return user.Id;
    }
}

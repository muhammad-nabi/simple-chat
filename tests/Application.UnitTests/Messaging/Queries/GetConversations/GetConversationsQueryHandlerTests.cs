using Moq;
using NUnit.Framework;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.Queries.GetConversations;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;
using static SimpleChat.Application.UnitTests.Messaging.Commands.SendMessage.SendMessageCommandHandlerTests;

namespace SimpleChat.Application.UnitTests.Messaging.Queries.GetConversations;

public class GetConversationsQueryHandlerTests
{
    private Mock<IApplicationDbContext> _db = null!;
    private Mock<IUser> _currentUser = null!;
    private Mock<IIdentityService> _identityService = null!;
    private GetConversationsQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _db = new Mock<IApplicationDbContext>();
        _currentUser = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
        _currentUser.Setup(u => u.Id).Returns("user-1");

        _identityService.Setup(s => s.GetDisplayNamesByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>
            {
                { "user-1", "Alice" },
                { "user-2", "Bob" },
                { "user-3", "Charlie" }
            });

        _handler = new GetConversationsQueryHandler(_db.Object, _currentUser.Object, _identityService.Object);
    }

    [Test]
    public async Task Handle_NoConversations_ShouldReturnEmptyList()
    {
        // Arrange
        SetupParticipants();
        GetConversationsQuery query = new();

        // Act
        List<ConversationListDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Handle_WithConversations_ShouldReturnSortedByLastMessageAt()
    {
        // Arrange
        Conversation conv1 = CreateConversation(1, ConversationType.Private, null, DateTimeOffset.UtcNow.AddHours(-2));
        Conversation conv2 = CreateConversation(2, ConversationType.Private, null, DateTimeOffset.UtcNow.AddHours(-1));
        SetupParticipants(
            new ConversationParticipant { ConversationId = 1, UserId = "user-1", LastReadMessageId = null },
            new ConversationParticipant { ConversationId = 2, UserId = "user-1", LastReadMessageId = null },
            new ConversationParticipant { ConversationId = 1, UserId = "user-2" },
            new ConversationParticipant { ConversationId = 2, UserId = "user-3" });
        SetupConversations(conv1, conv2);
        SetupMessages(
            CreateMessage(1, 1, "user-2", "Hello from Bob"),
            CreateMessage(2, 2, "user-3", "Hello from Charlie"));

        GetConversationsQuery query = new();

        // Act
        List<ConversationListDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Id, Is.EqualTo(2)); // More recent conversation first
        Assert.That(result[1].Id, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_ShouldReturnOtherParticipantsWithDisplayNames()
    {
        // Arrange
        Conversation conv1 = CreateConversation(1, ConversationType.Private, null, DateTimeOffset.UtcNow);
        SetupParticipants(
            new ConversationParticipant { ConversationId = 1, UserId = "user-1", LastReadMessageId = null },
            new ConversationParticipant { ConversationId = 1, UserId = "user-2" });
        SetupConversations(conv1);
        SetupMessages(CreateMessage(1, 1, "user-2", "Hi there"));

        GetConversationsQuery query = new();

        // Act
        List<ConversationListDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].OtherParticipants, Has.Count.EqualTo(1));
        Assert.That(result[0].OtherParticipants[0].UserId, Is.EqualTo("user-2"));
        Assert.That(result[0].OtherParticipants[0].DisplayName, Is.EqualTo("Bob"));
    }

    [Test]
    public async Task Handle_ShouldReturnLastMessagePreview()
    {
        // Arrange
        Conversation conv1 = CreateConversation(1, ConversationType.Private, null, DateTimeOffset.UtcNow);
        SetupParticipants(
            new ConversationParticipant { ConversationId = 1, UserId = "user-1", LastReadMessageId = null },
            new ConversationParticipant { ConversationId = 1, UserId = "user-2" });
        SetupConversations(conv1);
        SetupMessages(
            CreateMessage(1, 1, "user-2", "First message"),
            CreateMessage(2, 1, "user-1", "Latest message"));

        GetConversationsQuery query = new();

        // Act
        List<ConversationListDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result[0].LastMessagePreview, Is.EqualTo("Latest message"));
    }

    [Test]
    public async Task Handle_ShouldComputeUnreadCount()
    {
        // Arrange
        Conversation conv1 = CreateConversation(1, ConversationType.Private, null, DateTimeOffset.UtcNow);
        SetupParticipants(
            new ConversationParticipant { ConversationId = 1, UserId = "user-1", LastReadMessageId = 1 },
            new ConversationParticipant { ConversationId = 1, UserId = "user-2" });
        SetupConversations(conv1);
        SetupMessages(
            CreateMessage(1, 1, "user-2", "Read message"),
            CreateMessage(2, 1, "user-2", "Unread 1"),
            CreateMessage(3, 1, "user-2", "Unread 2"));

        GetConversationsQuery query = new();

        // Act
        List<ConversationListDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result[0].UnreadCount, Is.EqualTo(2));
    }

    [Test]
    public async Task Handle_NullLastReadMessageId_ShouldCountAllAsUnread()
    {
        // Arrange
        Conversation conv1 = CreateConversation(1, ConversationType.Private, null, DateTimeOffset.UtcNow);
        SetupParticipants(
            new ConversationParticipant { ConversationId = 1, UserId = "user-1", LastReadMessageId = null },
            new ConversationParticipant { ConversationId = 1, UserId = "user-2" });
        SetupConversations(conv1);
        SetupMessages(
            CreateMessage(1, 1, "user-2", "Message 1"),
            CreateMessage(2, 1, "user-2", "Message 2"));

        GetConversationsQuery query = new();

        // Act
        List<ConversationListDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result[0].UnreadCount, Is.EqualTo(2));
    }

    [Test]
    public async Task Handle_ShouldReturnConversationType()
    {
        // Arrange
        Conversation conv1 = CreateConversation(1, ConversationType.Private, null, DateTimeOffset.UtcNow);
        SetupParticipants(
            new ConversationParticipant { ConversationId = 1, UserId = "user-1", LastReadMessageId = null },
            new ConversationParticipant { ConversationId = 1, UserId = "user-2" });
        SetupConversations(conv1);
        SetupMessages();

        GetConversationsQuery query = new();

        // Act
        List<ConversationListDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result[0].Type, Is.EqualTo("Private"));
    }

    [Test]
    public void Handle_NoUserId_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _currentUser.Setup(u => u.Id).Returns((string?)null);
        _handler = new GetConversationsQueryHandler(_db.Object, _currentUser.Object, _identityService.Object);

        GetConversationsQuery query = new();

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    private void SetupParticipants(params ConversationParticipant[] participants)
    {
        Microsoft.EntityFrameworkCore.DbSet<ConversationParticipant> mockSet = CreateMockDbSet(participants.ToList()).Object;
        _db.Setup(d => d.ConversationParticipants).Returns(mockSet);
    }

    private void SetupConversations(params Conversation[] conversations)
    {
        Microsoft.EntityFrameworkCore.DbSet<Conversation> mockSet = CreateMockDbSet(conversations.ToList()).Object;
        _db.Setup(d => d.Conversations).Returns(mockSet);
    }

    private void SetupMessages(params Message[] messages)
    {
        Microsoft.EntityFrameworkCore.DbSet<Message> mockSet = CreateMockDbSet(messages.ToList()).Object;
        _db.Setup(d => d.Messages).Returns(mockSet);
    }

    private static Conversation CreateConversation(long id, ConversationType type, string? name, DateTimeOffset? lastMessageAt)
    {
        Conversation conversation = new()
        {
            Type = type,
            Name = name,
            CreatedById = "user-1",
            LastMessageAt = lastMessageAt,
        };
        SetEntityId(conversation, id);
        return conversation;
    }

    private static Message CreateMessage(long id, long conversationId, string senderId, string content)
    {
        Message message = new()
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            SentAt = DateTimeOffset.UtcNow,
            MessageType = MessageType.Text,
        };
        SetEntityId(message, id);
        return message;
    }
}

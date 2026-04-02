using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using SimpleChat.Application.Common.Exceptions;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.Queries.GetMessageHistory;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;
using static SimpleChat.Application.UnitTests.Messaging.Commands.SendMessage.SendMessageCommandHandlerTests;

namespace SimpleChat.Application.UnitTests.Messaging.Queries.GetMessageHistory;

public class GetMessageHistoryQueryHandlerTests
{
    private Mock<IApplicationDbContext> _db = null!;
    private Mock<IUser> _currentUser = null!;
    private Mock<IIdentityService> _identityService = null!;
    private GetMessageHistoryQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _db = new Mock<IApplicationDbContext>();
        _currentUser = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
        _currentUser.Setup(u => u.Id).Returns("user-1");

        _identityService.Setup(s => s.GetDisplayNamesByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { { "user-1", "Alice" }, { "user-2", "Bob" } });

        _handler = new GetMessageHistoryQueryHandler(_db.Object, _currentUser.Object, _identityService.Object);
    }

    [Test]
    public async Task Handle_ValidRequest_ShouldReturnMessagesInDescendingOrder()
    {
        // Arrange
        GetMessageHistoryQuery query = new(1, null, 50);
        SetupParticipant();
        SetupMessages(CreateMessage(3, 1, "user-1", "Third"), CreateMessage(2, 1, "user-2", "Second"), CreateMessage(1, 1, "user-1", "First"));

        // Act
        MessageHistoryResponse result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result.Messages, Has.Count.EqualTo(3));
        Assert.That(result.Messages[0].Id, Is.EqualTo(3));
        Assert.That(result.Messages[1].Id, Is.EqualTo(2));
        Assert.That(result.Messages[2].Id, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_WithCursor_ShouldReturnMessagesBeforeCursor()
    {
        // Arrange
        GetMessageHistoryQuery query = new(1, 3, 50);
        SetupParticipant();
        SetupMessages(CreateMessage(3, 1, "user-1", "Third"), CreateMessage(2, 1, "user-2", "Second"), CreateMessage(1, 1, "user-1", "First"));

        // Act
        MessageHistoryResponse result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result.Messages.All(m => m.Id < 3), Is.True);
    }

    [Test]
    public async Task Handle_MoreMessagesExist_ShouldSetHasMoreAndNextCursor()
    {
        // Arrange - limit 2, but 3 messages exist
        GetMessageHistoryQuery query = new(1, null, 2);
        SetupParticipant();
        SetupMessages(CreateMessage(3, 1, "user-1", "Third"), CreateMessage(2, 1, "user-1", "Second"), CreateMessage(1, 1, "user-1", "First"));

        // Act
        MessageHistoryResponse result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result.HasMore, Is.True);
        Assert.That(result.NextCursor, Is.Not.Null);
        Assert.That(result.Messages, Has.Count.EqualTo(2));
    }

    [Test]
    public void Handle_NonParticipant_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        GetMessageHistoryQuery query = new(1, null, 50);

        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(new List<ConversationParticipant>());
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        // Act & Assert
        Assert.ThrowsAsync<ForbiddenAccessException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    [Test]
    public async Task Handle_DefaultLimit_ShouldBe50()
    {
        // Arrange
        GetMessageHistoryQuery query = new(1, null);
        Assert.That(query.Limit, Is.EqualTo(50));

        SetupParticipant();
        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        // Act
        MessageHistoryResponse result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result.HasMore, Is.False);
        Assert.That(result.NextCursor, Is.Null);
    }

    private void SetupParticipant()
    {
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(new List<ConversationParticipant>
        {
            new() { ConversationId = 1, UserId = "user-1" }
        });
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);
    }

    private void SetupMessages(params Message[] msgs)
    {
        Mock<DbSet<Message>> messageSet = CreateMockDbSet(msgs.ToList());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);
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

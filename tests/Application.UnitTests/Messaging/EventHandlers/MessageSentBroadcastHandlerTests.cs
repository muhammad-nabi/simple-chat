using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.EventHandlers;
using SimpleChat.Application.Messaging.Notifications;
using SimpleChat.Application.Messaging.Queries.GetMessageHistory;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.UnitTests.Messaging.EventHandlers;

public class MessageSentBroadcastHandlerTests
{
    private Mock<IMessageBroadcaster> _broadcaster = null!;
    private Mock<IIdentityService> _identityService = null!;
    private Mock<ILogger<MessageSentBroadcastHandler>> _logger = null!;
    private MessageSentBroadcastHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _broadcaster = new Mock<IMessageBroadcaster>();
        _identityService = new Mock<IIdentityService>();
        _logger = new Mock<ILogger<MessageSentBroadcastHandler>>();
        _handler = new MessageSentBroadcastHandler(_broadcaster.Object, _identityService.Object, _logger.Object);
    }

    [Test]
    public async Task Handle_ShouldBroadcastMessageDtoWithCorrectConversationId()
    {
        // Arrange
        Message message = CreateMessage(42, 10, "sender-1", "Hello!");
        MessageSent notification = new(message, 10);

        _identityService
            .Setup(s => s.GetDisplayNamesByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.Contains("sender-1")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { { "sender-1", "Alice" } });

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _broadcaster.Verify(
            b => b.BroadcastMessageAsync(
                It.Is<MessageDto>(dto =>
                    dto.Id == 42 &&
                    dto.ConversationId == 10 &&
                    dto.SenderId == "sender-1" &&
                    dto.SenderDisplayName == "Alice" &&
                    dto.Content == "Hello!" &&
                    dto.MessageType == "Text"),
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ShouldResolveSenderDisplayNameViaIdentityService()
    {
        // Arrange
        Message message = CreateMessage(1, 5, "user-abc", "Test");
        MessageSent notification = new(message, 5);

        _identityService
            .Setup(s => s.GetDisplayNamesByIdsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { { "user-abc", "Bob Smith" } });

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _identityService.Verify(
            s => s.GetDisplayNamesByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.Contains("user-abc")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _broadcaster.Verify(
            b => b.BroadcastMessageAsync(
                It.Is<MessageDto>(dto => dto.SenderDisplayName == "Bob Smith"),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_WhenDisplayNameNotFound_ShouldUseUnknown()
    {
        // Arrange
        Message message = CreateMessage(1, 5, "unknown-user", "Test");
        MessageSent notification = new(message, 5);

        _identityService
            .Setup(s => s.GetDisplayNamesByIdsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _broadcaster.Verify(
            b => b.BroadcastMessageAsync(
                It.Is<MessageDto>(dto => dto.SenderDisplayName == "Unknown"),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
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

        // Set Id via backing field since the property has private set (BaseEntity)
        typeof(SimpleChat.Domain.Common.BaseEntity)
            .GetProperty("Id")!
            .GetSetMethod(nonPublic: true)!
            .Invoke(message, new object[] { id });

        return message;
    }
}

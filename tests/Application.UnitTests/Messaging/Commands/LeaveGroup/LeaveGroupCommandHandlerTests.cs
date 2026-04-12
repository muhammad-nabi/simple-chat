using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.Commands.LeaveGroup;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;
using static SimpleChat.Application.UnitTests.Messaging.Commands.SendMessage.SendMessageCommandHandlerTests;

namespace SimpleChat.Application.UnitTests.Messaging.Commands.LeaveGroup;

public class LeaveGroupCommandHandlerTests
{
    private Mock<IApplicationDbContext> _db = null!;
    private Mock<IUser> _currentUser = null!;
    private Mock<IIdentityService> _identityService = null!;
    private LeaveGroupCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _db = new Mock<IApplicationDbContext>();
        _currentUser = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
        _currentUser.Setup(u => u.Id).Returns("user-1");
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _identityService.Setup(s => s.GetDisplayNamesByIdsAsync(
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { { "user-1", "Test User" } });

        _handler = new LeaveGroupCommandHandler(_db.Object, _currentUser.Object, _identityService.Object);
    }

    [Test]
    public async Task Handle_ValidLeave_ShouldRemoveParticipantAndAddSystemMessage()
    {
        // Arrange
        Conversation group = new() { Type = ConversationType.Group, Name = "Team" };
        SetEntityId(group, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        ConversationParticipant existingParticipant = new() { ConversationId = 1, UserId = "user-1" };
        List<ConversationParticipant> participants = new() { existingParticipant };
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(participants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        LeaveGroupCommand command = new(ConversationId: 1);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert — participant removed
        participantSet.Verify(p => p.Remove(It.Is<ConversationParticipant>(cp =>
            cp.ConversationId == 1 && cp.UserId == "user-1")), Times.Once);

        // Assert — system message added
        messageSet.Verify(m => m.Add(It.Is<Message>(msg =>
            msg.ConversationId == 1 &&
            msg.Content == "Test User left the group" &&
            msg.MessageType == MessageType.System)), Times.Once);

        _db.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Handle_ConversationNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation>());
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        LeaveGroupCommand command = new(ConversationId: 999);

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public void Handle_PrivateConversation_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        Conversation privateConv = new() { Type = ConversationType.Private };
        SetEntityId(privateConv, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { privateConv });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        LeaveGroupCommand command = new(ConversationId: 1);

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.ForbiddenAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public void Handle_NonParticipant_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        Conversation group = new() { Type = ConversationType.Group, Name = "Team" };
        SetEntityId(group, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        // user-1 is NOT in participants
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(new List<ConversationParticipant>());
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        LeaveGroupCommand command = new(ConversationId: 1);

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.ForbiddenAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public async Task Handle_LastParticipantLeaves_ShouldPreserveConversation()
    {
        // Arrange — only one participant
        Conversation group = new() { Type = ConversationType.Group, Name = "Team" };
        SetEntityId(group, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        ConversationParticipant lastParticipant = new() { ConversationId = 1, UserId = "user-1" };
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(new List<ConversationParticipant> { lastParticipant });
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        LeaveGroupCommand command = new(ConversationId: 1);

        // Act — should succeed, conversation NOT deleted
        await _handler.Handle(command, CancellationToken.None);

        // Assert — participant removed but conversation still exists (no Remove on conversation set)
        participantSet.Verify(p => p.Remove(It.IsAny<ConversationParticipant>()), Times.Once);
        conversationSet.Verify(c => c.Remove(It.IsAny<Conversation>()), Times.Never);
        _db.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_ShouldUpdateConversationLastMessageAt()
    {
        // Arrange
        Conversation group = new() { Type = ConversationType.Group, Name = "Team", LastMessageAt = DateTimeOffset.UtcNow.AddHours(-1) };
        SetEntityId(group, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        ConversationParticipant participant = new() { ConversationId = 1, UserId = "user-1" };
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(new List<ConversationParticipant> { participant });
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        LeaveGroupCommand command = new(ConversationId: 1);
        DateTimeOffset before = DateTimeOffset.UtcNow;

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        group.LastMessageAt.ShouldNotBeNull();
        group.LastMessageAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.Commands.InviteToGroup;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;
using static SimpleChat.Application.UnitTests.Messaging.Commands.SendMessage.SendMessageCommandHandlerTests;

namespace SimpleChat.Application.UnitTests.Messaging.Commands.InviteToGroup;

public class InviteToGroupCommandHandlerTests
{
    private Mock<IApplicationDbContext> _db = null!;
    private Mock<IUser> _currentUser = null!;
    private Mock<IIdentityService> _identityService = null!;
    private InviteToGroupCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _db = new Mock<IApplicationDbContext>();
        _currentUser = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
        _currentUser.Setup(u => u.Id).Returns("inviter-1");
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _identityService.Setup(s => s.UserExistsAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _identityService.Setup(s => s.GetDisplayNamesByIdsAsync(
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>
            {
                { "inviter-1", "Inviter" },
                { "user-2", "Bob" },
                { "user-3", "Charlie" },
            });

        _handler = new InviteToGroupCommandHandler(_db.Object, _currentUser.Object, _identityService.Object);
    }

    [Test]
    public async Task Handle_SingleInvite_ShouldAddParticipantAndSystemMessage()
    {
        // Arrange
        Conversation group = new() { Type = ConversationType.Group, Name = "Team" };
        SetEntityId(group, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        List<ConversationParticipant> existingParticipants = new()
        {
            new() { ConversationId = 1, UserId = "inviter-1" },
        };
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(existingParticipants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        InviteToGroupCommand command = new(ConversationId: 1, UserIds: new List<string> { "user-2" });

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        participantSet.Verify(p => p.Add(It.Is<ConversationParticipant>(cp =>
            cp.ConversationId == 1 && cp.UserId == "user-2")), Times.Once);

        messageSet.Verify(m => m.Add(It.Is<Message>(msg =>
            msg.ConversationId == 1 &&
            msg.Content == "Inviter added Bob" &&
            msg.MessageType == MessageType.System)), Times.Once);

        _db.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_MultipleInvites_ShouldAddAllParticipantsAndSystemMessages()
    {
        // Arrange
        Conversation group = new() { Type = ConversationType.Group, Name = "Team" };
        SetEntityId(group, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        List<ConversationParticipant> existingParticipants = new()
        {
            new() { ConversationId = 1, UserId = "inviter-1" },
        };
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(existingParticipants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        InviteToGroupCommand command = new(ConversationId: 1, UserIds: new List<string> { "user-2", "user-3" });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — two participants added
        participantSet.Verify(p => p.Add(It.Is<ConversationParticipant>(cp =>
            cp.UserId == "user-2")), Times.Once);
        participantSet.Verify(p => p.Add(It.Is<ConversationParticipant>(cp =>
            cp.UserId == "user-3")), Times.Once);

        // Assert — two system messages added
        messageSet.Verify(m => m.Add(It.Is<Message>(msg =>
            msg.Content == "Inviter added Bob")), Times.Once);
        messageSet.Verify(m => m.Add(It.Is<Message>(msg =>
            msg.Content == "Inviter added Charlie")), Times.Once);

        _db.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_AlreadyParticipant_ShouldFilterSilently()
    {
        // Arrange
        Conversation group = new() { Type = ConversationType.Group, Name = "Team" };
        SetEntityId(group, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        List<ConversationParticipant> existingParticipants = new()
        {
            new() { ConversationId = 1, UserId = "inviter-1" },
            new() { ConversationId = 1, UserId = "user-2" },
        };
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(existingParticipants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        InviteToGroupCommand command = new(ConversationId: 1, UserIds: new List<string> { "user-2" });

        // Act — should not throw, silently filtered
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert — no participant or message added, no save
        participantSet.Verify(p => p.Add(It.IsAny<ConversationParticipant>()), Times.Never);
        _db.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Handle_ConversationNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation>());
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        InviteToGroupCommand command = new(ConversationId: 999, UserIds: new List<string> { "user-2" });

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

        InviteToGroupCommand command = new(ConversationId: 1, UserIds: new List<string> { "user-2" });

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.ForbiddenAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public void Handle_NonParticipantInviter_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        Conversation group = new() { Type = ConversationType.Group, Name = "Team" };
        SetEntityId(group, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        // Inviter (inviter-1) is NOT in the participants
        List<ConversationParticipant> existingParticipants = new()
        {
            new() { ConversationId = 1, UserId = "other-user" },
        };
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(existingParticipants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        InviteToGroupCommand command = new(ConversationId: 1, UserIds: new List<string> { "user-2" });

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.ForbiddenAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public void Handle_NonExistentUser_ShouldThrowNotFoundException()
    {
        // Arrange
        Conversation group = new() { Type = ConversationType.Group, Name = "Team" };
        SetEntityId(group, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        List<ConversationParticipant> existingParticipants = new()
        {
            new() { ConversationId = 1, UserId = "inviter-1" },
        };
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(existingParticipants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        // Override: user-2 does not exist
        _identityService.Setup(s => s.UserExistsAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        InviteToGroupCommand command = new(ConversationId: 1, UserIds: new List<string> { "nonexistent" });

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public async Task Handle_ShouldUpdateConversationLastMessageAt()
    {
        // Arrange
        Conversation group = new() { Type = ConversationType.Group, Name = "Team", LastMessageAt = DateTimeOffset.UtcNow.AddHours(-1) };
        SetEntityId(group, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        List<ConversationParticipant> existingParticipants = new()
        {
            new() { ConversationId = 1, UserId = "inviter-1" },
        };
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(existingParticipants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        InviteToGroupCommand command = new(ConversationId: 1, UserIds: new List<string> { "user-2" });
        DateTimeOffset before = DateTimeOffset.UtcNow;

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        group.LastMessageAt.ShouldNotBeNull();
        group.LastMessageAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }
}

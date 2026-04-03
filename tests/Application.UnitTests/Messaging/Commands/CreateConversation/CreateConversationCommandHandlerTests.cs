using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.Commands.CreateConversation;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;
using static SimpleChat.Application.UnitTests.Messaging.Commands.SendMessage.SendMessageCommandHandlerTests;

namespace SimpleChat.Application.UnitTests.Messaging.Commands.CreateConversation;

public class CreateConversationCommandHandlerTests
{
    private Mock<IApplicationDbContext> _db = null!;
    private Mock<IUser> _currentUser = null!;
    private Mock<IIdentityService> _identityService = null!;
    private CreateConversationCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _db = new Mock<IApplicationDbContext>();
        _currentUser = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
        _currentUser.Setup(u => u.Id).Returns("user-1");
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateConversationCommandHandler(_db.Object, _currentUser.Object, _identityService.Object);
    }

    // --- Private conversation tests (backward compatibility) ---

    [Test]
    public async Task Handle_NewPrivateConversation_ShouldCreateAndReturnId()
    {
        // Arrange
        CreateConversationCommand command = new(OtherUserId: "user-2");
        _identityService.Setup(s => s.UserExistsAsync("user-2", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation>());
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(new List<ConversationParticipant>());
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        // Act
        long result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        conversationSet.Verify(c => c.Add(It.Is<Conversation>(conv =>
            conv.Type == ConversationType.Private &&
            conv.Name == null &&
            conv.CreatedById == "user-1" &&
            conv.Participants.Count == 2 &&
            conv.Participants.Any(p => p.UserId == "user-1") &&
            conv.Participants.Any(p => p.UserId == "user-2"))), Times.Once);
    }

    [Test]
    public async Task Handle_ExistingPrivateConversation_ShouldReturnExistingId()
    {
        // Arrange
        CreateConversationCommand command = new(OtherUserId: "user-2");
        _identityService.Setup(s => s.UserExistsAsync("user-2", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Conversation existingConversation = new()
        {
            Type = ConversationType.Private,
            Participants = new List<ConversationParticipant>
            {
                new() { ConversationId = 42, UserId = "user-1" },
                new() { ConversationId = 42, UserId = "user-2" }
            }
        };
        SetEntityId(existingConversation, 42);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { existingConversation });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        // Act
        long result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(42));
        conversationSet.Verify(c => c.Add(It.IsAny<Conversation>()), Times.Never);
    }

    [Test]
    public void Handle_SelfConversation_ShouldThrowValidationException()
    {
        // Arrange
        CreateConversationCommand command = new(OtherUserId: "user-1");

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public void Handle_NonExistentUser_ShouldThrowNotFoundException()
    {
        // Arrange
        CreateConversationCommand command = new(OtherUserId: "nonexistent-user");
        _identityService.Setup(s => s.UserExistsAsync("nonexistent-user", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    // --- Group conversation tests ---

    [Test]
    public async Task Handle_NewGroupConversation_ShouldCreateWithCorrectTypeAndName()
    {
        // Arrange
        CreateConversationCommand command = new(
            ParticipantIds: new List<string> { "user-2", "user-3" },
            GroupName: "Engineering Team");

        _identityService.Setup(s => s.UserExistsAsync("user-2", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _identityService.Setup(s => s.UserExistsAsync("user-3", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation>());
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        // Act
        long result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        conversationSet.Verify(c => c.Add(It.Is<Conversation>(conv =>
            conv.Type == ConversationType.Group &&
            conv.Name == "Engineering Team" &&
            conv.CreatedById == "user-1" &&
            conv.Participants.Count == 3 &&
            conv.Participants.Any(p => p.UserId == "user-1") &&
            conv.Participants.Any(p => p.UserId == "user-2") &&
            conv.Participants.Any(p => p.UserId == "user-3"))), Times.Once);
        _db.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Handle_GroupConversation_NonExistentParticipant_ShouldThrowNotFoundException()
    {
        // Arrange
        CreateConversationCommand command = new(
            ParticipantIds: new List<string> { "user-2", "nonexistent-user" },
            GroupName: "Test Group");

        _identityService.Setup(s => s.UserExistsAsync("user-2", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _identityService.Setup(s => s.UserExistsAsync("nonexistent-user", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public void Handle_GroupConversation_CreatorInParticipantList_ShouldThrowValidationException()
    {
        // Arrange
        CreateConversationCommand command = new(
            ParticipantIds: new List<string> { "user-1", "user-2" },
            GroupName: "Test Group");

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public async Task Handle_GroupConversation_CreatorAddedAsParticipant()
    {
        // Arrange
        CreateConversationCommand command = new(
            ParticipantIds: new List<string> { "user-2", "user-3" },
            GroupName: "My Group");

        _identityService.Setup(s => s.UserExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation>());
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — creator is always added as a participant
        conversationSet.Verify(c => c.Add(It.Is<Conversation>(conv =>
            conv.Participants.Any(p => p.UserId == "user-1"))), Times.Once);
    }
}

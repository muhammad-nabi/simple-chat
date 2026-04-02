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

    [Test]
    public async Task Handle_NewConversation_ShouldCreateAndReturnId()
    {
        // Arrange
        CreateConversationCommand command = new("user-2");
        _identityService.Setup(s => s.UserExistsAsync("user-2", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation>());
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(new List<ConversationParticipant>());
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        // Act
        long result = await _handler.Handle(command, CancellationToken.None);

        // Assert — participants added via navigation property, not DbSet.Add
        conversationSet.Verify(c => c.Add(It.Is<Conversation>(conv =>
            conv.Type == ConversationType.Private &&
            conv.Name == null &&
            conv.CreatedById == "user-1" &&
            conv.Participants.Count == 2 &&
            conv.Participants.Any(p => p.UserId == "user-1") &&
            conv.Participants.Any(p => p.UserId == "user-2"))), Times.Once);
    }

    [Test]
    public async Task Handle_ExistingConversation_ShouldReturnExistingId()
    {
        // Arrange
        CreateConversationCommand command = new("user-2");
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
        CreateConversationCommand command = new("user-1");

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public void Handle_NonExistentUser_ShouldThrowNotFoundException()
    {
        // Arrange
        CreateConversationCommand command = new("nonexistent-user");
        _identityService.Setup(s => s.UserExistsAsync("nonexistent-user", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}

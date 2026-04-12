using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.Queries.GetGroupMembers;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;
using static SimpleChat.Application.UnitTests.Messaging.Commands.SendMessage.SendMessageCommandHandlerTests;

namespace SimpleChat.Application.UnitTests.Messaging.Queries.GetGroupMembers;

public class GetGroupMembersQueryHandlerTests
{
    private Mock<IApplicationDbContext> _db = null!;
    private Mock<IUser> _currentUser = null!;
    private Mock<IIdentityService> _identityService = null!;
    private GetGroupMembersQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _db = new Mock<IApplicationDbContext>();
        _currentUser = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
        _currentUser.Setup(u => u.Id).Returns("user-1");

        _handler = new GetGroupMembersQueryHandler(_db.Object, _currentUser.Object, _identityService.Object);
    }

    [Test]
    public async Task Handle_ValidGroupConversation_ShouldReturnAllMembersWithDisplayNames()
    {
        // Arrange
        Conversation group = new() { Type = ConversationType.Group, Name = "Team" };
        SetEntityId(group, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<ConversationParticipant> participants = new()
        {
            new() { ConversationId = 1, UserId = "user-1", JoinedAt = now.AddHours(-2) },
            new() { ConversationId = 1, UserId = "user-2", JoinedAt = now.AddHours(-1) },
            new() { ConversationId = 1, UserId = "user-3", JoinedAt = now },
        };
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(participants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        _identityService.Setup(s => s.GetDisplayNamesByIdsAsync(
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>
            {
                { "user-1", "Alice" },
                { "user-2", "Bob" },
                { "user-3", "Charlie" },
            });

        GetGroupMembersQuery query = new(ConversationId: 1);

        // Act
        List<GroupMemberDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(3);
        result[0].UserId.ShouldBe("user-1");
        result[0].DisplayName.ShouldBe("Alice");
        result[1].UserId.ShouldBe("user-2");
        result[1].DisplayName.ShouldBe("Bob");
        result[2].UserId.ShouldBe("user-3");
        result[2].DisplayName.ShouldBe("Charlie");
    }

    [Test]
    public void Handle_ConversationNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation>());
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        GetGroupMembersQuery query = new(ConversationId: 999);

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.NotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    [Test]
    public void Handle_PrivateConversation_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        Conversation privateConv = new() { Type = ConversationType.Private };
        SetEntityId(privateConv, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { privateConv });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        GetGroupMembersQuery query = new(ConversationId: 1);

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.ForbiddenAccessException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    [Test]
    public void Handle_NonParticipant_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        Conversation group = new() { Type = ConversationType.Group, Name = "Team" };
        SetEntityId(group, 1);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        // Current user (user-1) is NOT in the participants list
        List<ConversationParticipant> participants = new()
        {
            new() { ConversationId = 1, UserId = "user-2", JoinedAt = DateTimeOffset.UtcNow },
        };
        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(participants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        GetGroupMembersQuery query = new(ConversationId: 1);

        // Act & Assert
        Assert.ThrowsAsync<SimpleChat.Application.Common.Exceptions.ForbiddenAccessException>(
            () => _handler.Handle(query, CancellationToken.None));
    }
}

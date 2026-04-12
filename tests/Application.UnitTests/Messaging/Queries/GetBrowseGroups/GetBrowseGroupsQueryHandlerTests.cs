using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.Queries.GetBrowseGroups;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;
using static SimpleChat.Application.UnitTests.Messaging.Commands.SendMessage.SendMessageCommandHandlerTests;

namespace SimpleChat.Application.UnitTests.Messaging.Queries.GetBrowseGroups;

public class GetBrowseGroupsQueryHandlerTests
{
    private Mock<IApplicationDbContext> _db = null!;
    private Mock<IUser> _currentUser = null!;
    private GetBrowseGroupsQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _db = new Mock<IApplicationDbContext>();
        _currentUser = new Mock<IUser>();
        _currentUser.Setup(u => u.Id).Returns("user-1");

        _handler = new GetBrowseGroupsQueryHandler(_db.Object, _currentUser.Object);
    }

    [Test]
    public async Task Handle_ShouldReturnGroupsUserIsNotIn()
    {
        // Arrange
        Conversation group1 = new() { Type = ConversationType.Group, Name = "Team Alpha", LastMessageAt = DateTimeOffset.UtcNow };
        SetEntityId(group1, 1);

        Conversation group2 = new() { Type = ConversationType.Group, Name = "Team Beta", LastMessageAt = DateTimeOffset.UtcNow.AddMinutes(-5) };
        SetEntityId(group2, 2);

        Conversation group3 = new() { Type = ConversationType.Group, Name = "My Group", LastMessageAt = DateTimeOffset.UtcNow.AddMinutes(-10) };
        SetEntityId(group3, 3);

        // User is a participant in group3 only
        List<ConversationParticipant> userParticipations = new()
        {
            new() { ConversationId = 3, UserId = "user-1" }
        };

        // All participants for counting
        List<ConversationParticipant> allParticipants = new()
        {
            new() { ConversationId = 1, UserId = "user-2" },
            new() { ConversationId = 1, UserId = "user-3" },
            new() { ConversationId = 2, UserId = "user-2" },
            new() { ConversationId = 2, UserId = "user-3" },
            new() { ConversationId = 2, UserId = "user-4" },
            new() { ConversationId = 3, UserId = "user-1" },
            new() { ConversationId = 3, UserId = "user-2" },
        };

        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(allParticipants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group1, group2, group3 });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        // Act
        List<BrowseGroupDto> result = await _handler.Handle(new GetBrowseGroupsQuery(), CancellationToken.None);

        // Assert — should return group1 and group2, not group3 (user is in group3)
        result.Count.ShouldBe(2);
        result.ShouldContain(g => g.Id == 1 && g.Name == "Team Alpha");
        result.ShouldContain(g => g.Id == 2 && g.Name == "Team Beta");
        result.ShouldNotContain(g => g.Id == 3);
    }

    [Test]
    public async Task Handle_ShouldReturnCorrectParticipantCount()
    {
        // Arrange
        Conversation group1 = new() { Type = ConversationType.Group, Name = "Team Alpha", LastMessageAt = DateTimeOffset.UtcNow };
        SetEntityId(group1, 1);

        List<ConversationParticipant> allParticipants = new()
        {
            new() { ConversationId = 1, UserId = "user-2" },
            new() { ConversationId = 1, UserId = "user-3" },
            new() { ConversationId = 1, UserId = "user-4" },
        };

        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(allParticipants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group1 });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        // Act
        List<BrowseGroupDto> result = await _handler.Handle(new GetBrowseGroupsQuery(), CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result[0].ParticipantCount.ShouldBe(3);
    }

    [Test]
    public async Task Handle_ShouldExcludePrivateConversations()
    {
        // Arrange
        Conversation privateConv = new() { Type = ConversationType.Private, Name = null };
        SetEntityId(privateConv, 1);

        Conversation groupConv = new() { Type = ConversationType.Group, Name = "Public Group", LastMessageAt = DateTimeOffset.UtcNow };
        SetEntityId(groupConv, 2);

        List<ConversationParticipant> allParticipants = new()
        {
            new() { ConversationId = 2, UserId = "user-2" },
        };

        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(allParticipants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { privateConv, groupConv });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        // Act
        List<BrowseGroupDto> result = await _handler.Handle(new GetBrowseGroupsQuery(), CancellationToken.None);

        // Assert — only the group, not the private conversation
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Public Group");
    }

    [Test]
    public async Task Handle_NoAvailableGroups_ShouldReturnEmptyList()
    {
        // Arrange — user is in the only existing group
        Conversation group1 = new() { Type = ConversationType.Group, Name = "Only Group" };
        SetEntityId(group1, 1);

        List<ConversationParticipant> allParticipants = new()
        {
            new() { ConversationId = 1, UserId = "user-1" },
        };

        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(allParticipants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group1 });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        // Act
        List<BrowseGroupDto> result = await _handler.Handle(new GetBrowseGroupsQuery(), CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ShouldOrderByLastMessageAtDescending()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Conversation olderGroup = new() { Type = ConversationType.Group, Name = "Older", LastMessageAt = now.AddHours(-2) };
        SetEntityId(olderGroup, 1);

        Conversation newerGroup = new() { Type = ConversationType.Group, Name = "Newer", LastMessageAt = now };
        SetEntityId(newerGroup, 2);

        List<ConversationParticipant> allParticipants = new()
        {
            new() { ConversationId = 1, UserId = "user-2" },
            new() { ConversationId = 2, UserId = "user-2" },
        };

        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(allParticipants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { olderGroup, newerGroup });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        // Act
        List<BrowseGroupDto> result = await _handler.Handle(new GetBrowseGroupsQuery(), CancellationToken.None);

        // Assert — newer group first
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Newer");
        result[1].Name.ShouldBe("Older");
    }

    [Test]
    public async Task Handle_ShouldIncludeLastMessagePreview()
    {
        // Arrange
        Conversation group1 = new() { Type = ConversationType.Group, Name = "Team", LastMessageAt = DateTimeOffset.UtcNow };
        SetEntityId(group1, 1);

        Message latestMessage = new()
        {
            ConversationId = 1,
            SenderId = "user-2",
            Content = "Hello everyone!",
            SentAt = DateTimeOffset.UtcNow,
            MessageType = MessageType.Text,
        };
        SetEntityId(latestMessage, 10);

        List<ConversationParticipant> allParticipants = new()
        {
            new() { ConversationId = 1, UserId = "user-2" },
        };

        Mock<DbSet<ConversationParticipant>> participantSet = CreateMockDbSet(allParticipants);
        _db.Setup(d => d.ConversationParticipants).Returns(participantSet.Object);

        Mock<DbSet<Conversation>> conversationSet = CreateMockDbSet(new List<Conversation> { group1 });
        _db.Setup(d => d.Conversations).Returns(conversationSet.Object);

        Mock<DbSet<Message>> messageSet = CreateMockDbSet(new List<Message> { latestMessage });
        _db.Setup(d => d.Messages).Returns(messageSet.Object);

        // Act
        List<BrowseGroupDto> result = await _handler.Handle(new GetBrowseGroupsQuery(), CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result[0].LastMessagePreview.ShouldBe("Hello everyone!");
    }
}

using System.Linq.Expressions;
using System.Reflection;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using NUnit.Framework;
using SimpleChat.Application.Common.Exceptions;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.Commands.SendMessage;
using SimpleChat.Application.Messaging.Notifications;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.UnitTests.Messaging.Commands.SendMessage;

public class SendMessageCommandHandlerTests
{
    private Mock<IApplicationDbContext> _db = null!;
    private Mock<IUser> _currentUser = null!;
    private Mock<IPublisher> _publisher = null!;
    private SendMessageCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _db = new Mock<IApplicationDbContext>();
        _currentUser = new Mock<IUser>();
        _publisher = new Mock<IPublisher>();
        _currentUser.Setup(u => u.Id).Returns("user-1");
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Default empty Messages DbSet so .Add() doesn't throw null ref
        Mock<DbSet<Message>> defaultMessages = CreateMockDbSet(new List<Message>());
        _db.Setup(d => d.Messages).Returns(defaultMessages.Object);

        _handler = new SendMessageCommandHandler(_db.Object, _currentUser.Object, _publisher.Object);
    }

    [Test]
    public async Task Handle_ValidMessage_ShouldPersistAndReturnId()
    {
        // Arrange
        SendMessageCommand command = new(1, "Hello world");
        SetupParticipants(new ConversationParticipant { ConversationId = 1, UserId = "user-1" });
        SetupConversations(CreateConversation(1, ConversationType.Private));

        // Act
        long result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _db.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Handle_NonParticipant_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        SendMessageCommand command = new(1, "Hello");
        SetupParticipants(); // Empty

        // Act & Assert
        Assert.ThrowsAsync<ForbiddenAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public async Task Handle_ValidMessage_ShouldUpdateLastMessageAt()
    {
        // Arrange
        SendMessageCommand command = new(1, "Hello");
        SetupParticipants(new ConversationParticipant { ConversationId = 1, UserId = "user-1" });
        Conversation conversation = CreateConversation(1, ConversationType.Private);
        SetupConversations(conversation);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(conversation.LastMessageAt, Is.Not.Null);
    }

    [Test]
    public async Task Handle_ValidMessage_ShouldPublishMessageSent()
    {
        // Arrange
        SendMessageCommand command = new(1, "Hello");
        SetupParticipants(new ConversationParticipant { ConversationId = 1, UserId = "user-1" });
        SetupConversations(CreateConversation(1, ConversationType.Private));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _publisher.Verify(p => p.Publish(
            It.Is<MessageSent>(n => n.ConversationId == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupParticipants(params ConversationParticipant[] participants)
    {
        Mock<DbSet<ConversationParticipant>> mockSet = CreateMockDbSet(participants.ToList());
        _db.Setup(d => d.ConversationParticipants).Returns(mockSet.Object);
    }

    private void SetupConversations(params Conversation[] conversations)
    {
        Mock<DbSet<Conversation>> mockSet = CreateMockDbSet(conversations.ToList());
        _db.Setup(d => d.Conversations).Returns(mockSet.Object);
    }

    internal static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        IQueryable<T> queryable = data.AsQueryable();
        Mock<DbSet<T>> mockSet = new();
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
        return mockSet;
    }

    internal static void SetEntityId(SimpleChat.Domain.Common.BaseEntity entity, long id)
    {
        typeof(SimpleChat.Domain.Common.BaseEntity)
            .GetProperty("Id")!
            .SetValue(entity, id);
    }

    private static Conversation CreateConversation(long id, ConversationType type)
    {
        Conversation conversation = new() { Type = type };
        SetEntityId(conversation, id);
        return conversation;
    }
}

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        // TResult is Task<T> -- we need to execute synchronously and wrap in Task
        Type resultType = typeof(TResult).GetGenericArguments()[0];

        // Execute the expression against the in-memory provider
        object? result = _inner.Execute(expression);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { result })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}

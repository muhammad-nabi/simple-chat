using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using SimpleChat.Infrastructure.Services;
using StackExchange.Redis;

namespace SimpleChat.Application.UnitTests.Infrastructure.Services;

public class RedisCacheServiceTests
{
    private Mock<IConnectionMultiplexer> _redis = null!;
    private Mock<IDatabase> _db = null!;
    private Mock<ILogger<RedisCacheService>> _logger = null!;
    private RedisCacheService _service = null!;

    [SetUp]
    public void Setup()
    {
        _redis = new Mock<IConnectionMultiplexer>();
        _db = new Mock<IDatabase>();
        _logger = new Mock<ILogger<RedisCacheService>>();
        _redis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_db.Object);

        _service = new RedisCacheService(_redis.Object, _logger.Object);
    }

    [Test]
    public async Task GetAsync_CorruptPayload_ShouldReturnDefaultAndNotThrow()
    {
        // Arrange — a value that is not valid JSON for TestData
        _db.Setup(x => x.StringGetAsync(It.Is<RedisKey>(k => k == "poison"), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("not-valid-json"));

        // Act
        TestData? result = await _service.GetAsync<TestData>("poison");

        // Assert — treats poison key as missing rather than propagating JsonException
        result.ShouldBeNull();
    }

    [Test]
    public async Task GetAsync_KeyExists_ShouldDeserializeAndReturn()
    {
        // Arrange
        string json = JsonSerializer.Serialize(new TestData("hello"));
        _db.Setup(x => x.StringGetAsync(It.Is<RedisKey>(k => k == "test-key"), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(json));

        // Act
        TestData? result = await _service.GetAsync<TestData>("test-key");

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBe("hello");
    }

    [Test]
    public async Task GetAsync_KeyNotExists_ShouldReturnDefault()
    {
        // Arrange
        _db.Setup(x => x.StringGetAsync(It.Is<RedisKey>(k => k == "missing"), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        TestData? result = await _service.GetAsync<TestData>("missing");

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task SetAsync_WithExpiry_ShouldSerializeAndStoreWithTtl()
    {
        // Arrange
        TestData data = new("world");
        TimeSpan expiry = TimeSpan.FromSeconds(90);

        _db.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _service.SetAsync("key1", data, expiry);

        // Assert — verify StringSetAsync was called with TTL overload
        Moq.IInvocation invocation = _db.Invocations
            .Single(i => i.Method.Name == "StringSetAsync");
        invocation.Arguments[0].ToString().ShouldBe("key1");
        invocation.Arguments[1].ToString()!.ShouldContain("world");
    }

    [Test]
    public async Task SetAsync_WithoutExpiry_ShouldSerializeAndStore()
    {
        // Arrange
        TestData data = new("test");

        // Act
        await _service.SetAsync("key2", data);

        // Assert — verify StringSetAsync was called
        Moq.IInvocation invocation = _db.Invocations
            .Single(i => i.Method.Name == "StringSetAsync");
        invocation.Arguments[0].ToString().ShouldBe("key2");
        invocation.Arguments[1].ToString()!.ShouldContain("test");
    }

    [Test]
    public async Task DeleteAsync_ShouldCallKeyDelete()
    {
        // Act
        await _service.DeleteAsync("del-key");

        // Assert
        _db.Verify(x => x.KeyDeleteAsync(
            It.Is<RedisKey>(k => k == "del-key"),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Test]
    public async Task SetMembersAsync_ShouldReturnMembersAsSet()
    {
        // Arrange
        RedisValue[] members = { new("user1"), new("user2"), new("user3") };
        _db.Setup(x => x.SetMembersAsync(It.Is<RedisKey>(k => k == "online_users"), It.IsAny<CommandFlags>()))
            .ReturnsAsync(members);

        // Act
        IReadOnlySet<string> result = await _service.SetMembersAsync("online_users");

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldContain("user1");
        result.ShouldContain("user2");
        result.ShouldContain("user3");
    }

    [Test]
    public async Task SetMembersAsync_EmptySet_ShouldReturnEmptySet()
    {
        // Arrange
        _db.Setup(x => x.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(Array.Empty<RedisValue>());

        // Act
        IReadOnlySet<string> result = await _service.SetMembersAsync("empty-set");

        // Assert
        result.Count.ShouldBe(0);
    }

    [Test]
    public async Task SetAddAsync_ShouldCallSetAdd()
    {
        // Act
        await _service.SetAddAsync("my-set", "member1");

        // Assert
        _db.Verify(x => x.SetAddAsync(
            It.Is<RedisKey>(k => k == "my-set"),
            It.Is<RedisValue>(v => v == "member1"),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Test]
    public async Task SetRemoveAsync_ShouldCallSetRemove()
    {
        // Act
        await _service.SetRemoveAsync("my-set", "member1");

        // Assert
        _db.Verify(x => x.SetRemoveAsync(
            It.Is<RedisKey>(k => k == "my-set"),
            It.Is<RedisValue>(v => v == "member1"),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Test]
    public async Task KeyExistsAsync_KeyExists_ShouldReturnTrue()
    {
        // Arrange
        _db.Setup(x => x.KeyExistsAsync(It.Is<RedisKey>(k => k == "existing"), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        bool result = await _service.KeyExistsAsync("existing");

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public async Task KeyExistsAsync_KeyNotExists_ShouldReturnFalse()
    {
        // Arrange
        _db.Setup(x => x.KeyExistsAsync(It.Is<RedisKey>(k => k == "missing"), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.KeyExistsAsync("missing");

        // Assert
        result.ShouldBeFalse();
    }

    private record TestData(string Value);
}

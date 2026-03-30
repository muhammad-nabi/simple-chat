using NUnit.Framework;
using SimpleChat.Infrastructure.Identity;
using SimpleChat.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;

namespace SimpleChat.Application.UnitTests.Infrastructure.Services;

public class BcryptPasswordHasherTests
{
    private BcryptPasswordHasher _hasher = null!;
    private ApplicationUser _user = null!;

    [SetUp]
    public void Setup()
    {
        _hasher = new BcryptPasswordHasher();
        _user = new ApplicationUser { DisplayName = "Test", Email = "test@test.com", UserName = "test@test.com" };
    }

    [Test]
    public void HashPassword_ShouldReturnBcryptHash()
    {
        var hash = _hasher.HashPassword(_user, "password123");

        Assert.That(hash, Is.Not.Null.And.Not.Empty);
        Assert.That(hash, Does.StartWith("$2"));
    }

    [Test]
    public void VerifyHashedPassword_ShouldReturnSuccess_WhenPasswordMatches()
    {
        var hash = _hasher.HashPassword(_user, "password123");
        var result = _hasher.VerifyHashedPassword(_user, hash, "password123");

        Assert.That(result, Is.EqualTo(PasswordVerificationResult.Success));
    }

    [Test]
    public void VerifyHashedPassword_ShouldReturnFailed_WhenPasswordDoesNotMatch()
    {
        var hash = _hasher.HashPassword(_user, "password123");
        var result = _hasher.VerifyHashedPassword(_user, hash, "wrongpassword");

        Assert.That(result, Is.EqualTo(PasswordVerificationResult.Failed));
    }

    [Test]
    public void HashPassword_ShouldProduceDifferentHashes_ForSamePassword()
    {
        var hash1 = _hasher.HashPassword(_user, "password123");
        var hash2 = _hasher.HashPassword(_user, "password123");

        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }
}

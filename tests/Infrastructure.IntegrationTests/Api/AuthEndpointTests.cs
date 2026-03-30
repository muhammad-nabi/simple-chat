using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

namespace SimpleChat.Infrastructure.IntegrationTests.Api;

public class AuthEndpointTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        await _factory.InitialiseDatabaseAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Register_WithValidData_ReturnsOkWithAccessToken()
    {
        // Arrange
        var request = new { displayName = "Test User", email = "test@example.com", password = "password123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("accessToken").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(body.GetProperty("userId").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(body.GetProperty("displayName").GetString(), Is.EqualTo("Test User"));
        Assert.That(body.GetProperty("email").GetString(), Is.EqualTo("test@example.com"));
    }

    [Test]
    public async Task Register_FirstUser_GetsAdminRole()
    {
        // Arrange
        var request = new { displayName = "First User", email = "first@example.com", password = "password123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("role").GetString(), Is.EqualTo("Admin"));
    }

    [Test]
    public async Task Register_SecondUser_GetsMemberRole()
    {
        // Arrange — register first user
        await _client.PostAsJsonAsync("/api/auth/register",
            new { displayName = "First", email = "first@example.com", password = "password123" });

        // Act — register second user
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { displayName = "Second", email = "second@example.com", password = "password123" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("role").GetString(), Is.EqualTo("Member"));
    }

    [Test]
    public async Task Register_DuplicateEmail_Returns400()
    {
        // Arrange — register first user
        await _client.PostAsJsonAsync("/api/auth/register",
            new { displayName = "User One", email = "duplicate@example.com", password = "password123" });

        // Act — try to register with same email
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { displayName = "User Two", email = "duplicate@example.com", password = "password123" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_InvalidEmail_Returns400()
    {
        var request = new { displayName = "Test", email = "not-an-email", password = "password123" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_ShortPassword_Returns400()
    {
        var request = new { displayName = "Test", email = "test@example.com", password = "short" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_SetsRefreshTokenCookie()
    {
        var request = new { displayName = "Cookie Test", email = "cookie@example.com", password = "password123" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.Contains("Set-Cookie"), Is.True);
        var cookieHeader = response.Headers.GetValues("Set-Cookie").First();
        Assert.That(cookieHeader, Does.Contain("refresh_token="));
        Assert.That(cookieHeader, Does.Contain("httponly"));
    }
}

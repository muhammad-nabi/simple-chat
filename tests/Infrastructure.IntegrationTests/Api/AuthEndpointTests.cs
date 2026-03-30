using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        await _factory.ResetDatabaseAsync();
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

    // --- Login Tests ---

    private async Task RegisterUser(string email = "login@example.com", string password = "password123",
        string displayName = "Login User")
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { displayName, email, password });
        response.EnsureSuccessStatusCode();
    }

    [Test]
    public async Task Login_WithValidCredentials_ReturnsOkWithAccessToken()
    {
        // Arrange
        await RegisterUser();

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "login@example.com", password = "password123" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("accessToken").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(body.GetProperty("userId").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(body.GetProperty("displayName").GetString(), Is.EqualTo("Login User"));
        Assert.That(body.GetProperty("email").GetString(), Is.EqualTo("login@example.com"));
    }

    [Test]
    public async Task Login_WithValidCredentials_SetsRefreshTokenCookie()
    {
        // Arrange
        await RegisterUser("cookie-login@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "cookie-login@example.com", password = "password123" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.Contains("Set-Cookie"), Is.True);
        var cookieHeader = response.Headers.GetValues("Set-Cookie").First();
        Assert.That(cookieHeader, Does.Contain("refresh_token="));
        Assert.That(cookieHeader, Does.Contain("httponly"));
    }

    [Test]
    public async Task Login_WithWrongPassword_Returns401()
    {
        // Arrange
        await RegisterUser("wrong-pw@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "wrong-pw@example.com", password = "wrongpassword" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("detail").GetString(), Is.EqualTo("Invalid email or password."));
    }

    [Test]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@example.com", password = "password123" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("detail").GetString(), Is.EqualTo("Invalid email or password."));
    }

    [Test]
    public async Task Login_WithDeactivatedUser_Returns401()
    {
        // Arrange — register a user then deactivate them directly via the database
        await RegisterUser("deactivated@example.com");
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SimpleChat.Infrastructure.Data.ApplicationDbContext>();
            var user = await dbContext.Users
                .FirstAsync(u => u.Email == "deactivated@example.com");
            ((SimpleChat.Infrastructure.Identity.ApplicationUser)user).IsActive = false;
            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "deactivated@example.com", password = "password123" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Login_InvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "not-an-email", password = "password123" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}

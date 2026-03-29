using System.Net;

namespace SimpleChat.Infrastructure.IntegrationTests;

[TestFixture]
public class HealthCheckIntegrationTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new CustomWebApplicationFactory();
        await _factory.InitialiseDatabaseAsync();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Test]
    public async Task HealthReady_ReturnsOk_WhenMsSqlAndRedisAreAvailable()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task HealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}

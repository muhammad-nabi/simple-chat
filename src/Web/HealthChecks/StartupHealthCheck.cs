using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SimpleChat.Web.HealthChecks;

public class StartupHealthCheck : IHealthCheck
{
    private volatile bool _isReady;

    public void MarkReady() => _isReady = true;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_isReady
            ? HealthCheckResult.Healthy("Migrations complete")
            : HealthCheckResult.Unhealthy("Migrations pending"));
    }
}

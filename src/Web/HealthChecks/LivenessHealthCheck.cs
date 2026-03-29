using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SimpleChat.Web.HealthChecks;

public class LivenessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Process is alive"));
    }
}

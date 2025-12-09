namespace mark.davison.common.server.Health;

public sealed class LiveHealthCheck : IHealthCheck
{
    public const string Name = nameof(LiveHealthCheck);

    private readonly IApplicationHealthState _applicationHealthState;

    public LiveHealthCheck(IApplicationHealthState applicationHealthState)
    {
        _applicationHealthState = applicationHealthState;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_applicationHealthState.Ready.GetValueOrDefault())
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        return Task.FromResult(HealthCheckResult.Unhealthy());
    }
}
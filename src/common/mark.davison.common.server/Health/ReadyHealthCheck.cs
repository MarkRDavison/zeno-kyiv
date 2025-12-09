namespace mark.davison.common.server.Health;

public sealed class ReadyHealthCheck : IHealthCheck
{
    public const string Name = nameof(ReadyHealthCheck);

    private readonly IApplicationHealthState _applicationHealthState;

    public ReadyHealthCheck(IApplicationHealthState applicationHealthState)
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
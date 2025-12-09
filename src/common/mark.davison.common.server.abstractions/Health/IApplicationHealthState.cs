namespace mark.davison.common.server.abstractions.Health;

public interface IApplicationHealthState
{
    bool? Started { get; set; }
    bool? Ready { get; set; }
    bool? Healthy { get; set; }
    TaskCompletionSource ReadySource { get; }
}
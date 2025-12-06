namespace mark.davison.common.client.abstractions.CQRS;

public interface IActionDispatcher
{
    Task Dispatch<TAction>(TAction action, CancellationToken cancellation)
        where TAction : class, IAction<TAction>;

    Task Dispatch<TAction>(CancellationToken cancellation)
        where TAction : class, IAction<TAction>, new();

    Task<TActionResponse> Dispatch<TActionRequest, TActionResponse>(TActionRequest action, CancellationToken cancellation)
        where TActionRequest : class, IResponseAction<TActionRequest, TActionResponse>
        where TActionResponse : class;
    Task<TActionResponse> Dispatch<TActionRequest, TActionResponse>(CancellationToken cancellation)
        where TActionRequest : class, IResponseAction<TActionRequest, TActionResponse>, new()
        where TActionResponse : class;
}

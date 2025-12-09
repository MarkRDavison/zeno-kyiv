namespace mark.davison.common.client.web.CQRS;

public class ActionDispatcher : IActionDispatcher
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ActionDispatcher(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task Dispatch<TAction>(TAction action, CancellationToken cancellationToken)
        where TAction : class, IAction<TAction>
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IActionHandler<TAction>>();
        return handler.Handle(action, cancellationToken);
    }

    public Task Dispatch<TAction>(CancellationToken cancellationToken)
        where TAction : class, IAction<TAction>, new()
    {
        return Dispatch<TAction>(new TAction(), cancellationToken);
    }

    public Task<TActionResponse> Dispatch<TActionRequest, TActionResponse>(TActionRequest action, CancellationToken cancellationToken)
        where TActionRequest : class, IResponseAction<TActionRequest, TActionResponse>
        where TActionResponse : class
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IResponseActionHandler<TActionRequest, TActionResponse>>();
        return handler.Handle(action, cancellationToken);
    }

    public Task<TActionResponse> Dispatch<TActionRequest, TActionResponse>(CancellationToken cancellationToken)
        where TActionRequest : class, IResponseAction<TActionRequest, TActionResponse>, new()
        where TActionResponse : class
    {
        return Dispatch<TActionRequest, TActionResponse>(new TActionRequest(), cancellationToken);
    }
}
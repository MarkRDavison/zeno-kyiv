namespace mark.davison.common.client.web.CQRS;

public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public QueryDispatcher(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task<TQueryResult> Dispatch<TQuery, TQueryResult>(TQuery query, CancellationToken cancellationToken)
        where TQuery : class, IQuery<TQuery, TQueryResult>, new()
        where TQueryResult : class, new()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TQueryResult>>();
        return handler.Handle(query, cancellationToken);
    }

    public Task<TQueryResult> Dispatch<TQuery, TQueryResult>(CancellationToken cancellationToken)
        where TQuery : class, IQuery<TQuery, TQueryResult>, new()
        where TQueryResult : class, new()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TQueryResult>>();
        return handler.Handle(new TQuery(), cancellationToken);
    }

}

namespace mark.davison.common.client.abstractions.CQRS;

public interface IQueryDispatcher
{
    Task<TQueryResult> Dispatch<TQuery, TQueryResult>(TQuery query, CancellationToken cancellation)
        where TQuery : class, IQuery<TQuery, TQueryResult>
        where TQueryResult : class, new();

    Task<TQueryResult> Dispatch<TQuery, TQueryResult>(CancellationToken cancellation)
        where TQuery : class, IQuery<TQuery, TQueryResult>, new()
        where TQueryResult : class, new();
}
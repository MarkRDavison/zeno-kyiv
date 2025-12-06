namespace mark.davison.common.client.abstractions.CQRS;

public interface IQueryHandler<in TQuery, TQueryResult>
    where TQuery : class, IQuery<TQuery, TQueryResult>
    where TQueryResult : class, new()
{
    Task<TQueryResult> Handle(TQuery query, CancellationToken cancellation);
}
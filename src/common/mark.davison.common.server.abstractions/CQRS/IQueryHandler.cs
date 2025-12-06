namespace mark.davison.common.server.abstractions.CQRS;

public interface IQueryHandler<in TQuery, TQueryResult>
    where TQuery : class, IQuery<TQuery, TQueryResult>, new()
    where TQueryResult : class, new()
{
    Task<TQueryResult> Handle(TQuery query, ICurrentUserContext currentUserContext, CancellationToken cancellation);
}
namespace mark.davison.common.server.abstractions.CQRS;

public interface IQueryProcessor<TRequest, TResponse> where TRequest : class, IQuery<TRequest, TResponse> where TResponse : Response, new()
{
    Task<TResponse> ProcessAsync(TRequest request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken);
}

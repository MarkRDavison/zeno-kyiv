namespace mark.davison.common.server.abstractions.CQRS;

public interface IQueryValidator<TRequest, TResponse>
    where TRequest : class, IQuery<TRequest, TResponse>
    where TResponse : Response, new()
{
    public Task<TResponse> ValidateAsync(TRequest request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken);
}

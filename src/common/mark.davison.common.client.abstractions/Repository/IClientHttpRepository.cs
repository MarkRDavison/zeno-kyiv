namespace mark.davison.common.client.abstractions.Repository;

public interface IClientHttpRepository
{

    Task<TResponse> Get<TResponse, TRequest>(TRequest request, CancellationToken cancellationToken)
        where TRequest : class, IQuery<TRequest, TResponse>
        where TResponse : Response, new();

    Task<TResponse> Get<TResponse, TRequest>(CancellationToken cancellationToken)
        where TRequest : class, IQuery<TRequest, TResponse>, new()
        where TResponse : Response, new();

    Task<TResponse> Post<TResponse, TRequest>(TRequest request, CancellationToken cancellationToken)
        where TRequest : class, ICommand<TRequest, TResponse>
        where TResponse : Response, new();

    Task<TResponse> Post<TResponse, TRequest>(CancellationToken cancellationToken)
        where TRequest : class, ICommand<TRequest, TResponse>, new()
        where TResponse : Response, new();

    event EventHandler<InvalidHttpResponseEventArgs> OnInvalidHttpResponse;

}
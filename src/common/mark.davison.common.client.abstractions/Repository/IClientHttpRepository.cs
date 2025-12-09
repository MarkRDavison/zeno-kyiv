namespace mark.davison.common.client.abstractions.Repository;

public interface IClientHttpRepository
{

    Task<TResponse> Get<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : class, IQuery<TRequest, TResponse>
        where TResponse : Response, new();

    Task<TResponse> Get<TRequest, TResponse>(CancellationToken cancellationToken)
        where TRequest : class, IQuery<TRequest, TResponse>, new()
        where TResponse : Response, new();

    Task<TResponse> Post<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : class, ICommand<TRequest, TResponse>
        where TResponse : Response, new();

    Task<TResponse> Post<TRequest, TResponse>(CancellationToken cancellationToken)
        where TRequest : class, ICommand<TRequest, TResponse>, new()
        where TResponse : Response, new();

    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);

    event EventHandler<InvalidHttpResponseEventArgs> OnInvalidHttpResponse;

}
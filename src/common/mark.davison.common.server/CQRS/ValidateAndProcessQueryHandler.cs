namespace mark.davison.common.server.CQRS;

public class ValidateAndProcessQueryHandler<TRequest, TResponse>
    : IQueryHandler<TRequest, TResponse>
    where TRequest : class, IQuery<TRequest, TResponse>, new()
    where TResponse : Response, new()
{
    private readonly IQueryProcessor<TRequest, TResponse> _processor;
    private readonly IQueryValidator<TRequest, TResponse>? _validator;

    public ValidateAndProcessQueryHandler(
        IQueryProcessor<TRequest, TResponse> processor
    )
    {
        _processor = processor;
        _validator = null;
    }
    public ValidateAndProcessQueryHandler(
        IQueryProcessor<TRequest, TResponse> processor,
        IQueryValidator<TRequest, TResponse> validator)
    {
        _processor = processor;
        _validator = validator;
    }

    public async Task<TResponse> Handle(TRequest query, ICurrentUserContext currentUserContext, CancellationToken cancellation)
    {
        if (_validator != null)
        {
            var response = await _validator.ValidateAsync(query, currentUserContext, cancellation);

            if (!response.Success)
            {
                return response;
            }
        }

        return await _processor.ProcessAsync(query, currentUserContext, cancellation);
    }
}

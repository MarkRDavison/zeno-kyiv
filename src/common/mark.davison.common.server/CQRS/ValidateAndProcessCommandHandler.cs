namespace mark.davison.common.server.CQRS;

public class ValidateAndProcessCommandHandler<TRequest, TResponse>
    : ICommandHandler<TRequest, TResponse>
    where TRequest : class, ICommand<TRequest, TResponse>, new()
    where TResponse : Response, new()
{
    private readonly ICommandProcessor<TRequest, TResponse> _processor;
    private readonly ICommandValidator<TRequest, TResponse>? _validator;

    public ValidateAndProcessCommandHandler(
        ICommandProcessor<TRequest, TResponse> processor
    )
    {
        _processor = processor;
        _validator = null;
    }
    public ValidateAndProcessCommandHandler(
        ICommandProcessor<TRequest, TResponse> processor,
        ICommandValidator<TRequest, TResponse> validator)
    {
        _processor = processor;
        _validator = validator;
    }

    public async Task<TResponse> Handle(TRequest command, ICurrentUserContext currentUserContext, CancellationToken cancellation)
    {
        if (_validator != null)
        {
            var response = await _validator.ValidateAsync(command, currentUserContext, cancellation);

            if (!response.Success)
            {
                return response;
            }
        }

        return await _processor.ProcessAsync(command, currentUserContext, cancellation);
    }
}
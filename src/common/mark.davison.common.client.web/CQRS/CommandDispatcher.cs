namespace mark.davison.common.client.web.CQRS;

public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CommandDispatcher(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task<TCommandResult> Dispatch<TCommand, TCommandResult>(TCommand command, CancellationToken cancellationToken)
        where TCommand : class, ICommand<TCommand, TCommandResult>, new()
        where TCommandResult : class, new()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TCommandResult>>();
        return handler.Handle(command, cancellationToken);
    }

    public Task<TCommandResult> Dispatch<TCommand, TCommandResult>(CancellationToken cancellationToken)
        where TCommand : class, ICommand<TCommand, TCommandResult>, new()
        where TCommandResult : class, new()
    {
        return Dispatch<TCommand, TCommandResult>(new TCommand(), cancellationToken);
    }

}

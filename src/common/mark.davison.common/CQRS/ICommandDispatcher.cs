namespace mark.davison.common.CQRS;

public interface ICommandDispatcher
{

    Task<TCommandResult> Dispatch<TCommand, TCommandResult>(TCommand command, CancellationToken cancellation)
        where TCommand : class, ICommand<TCommand, TCommandResult>, new()
        where TCommandResult : class, new();

    Task<TCommandResult> Dispatch<TCommand, TCommandResult>(CancellationToken cancellation)
        where TCommand : class, ICommand<TCommand, TCommandResult>, new()
        where TCommandResult : class, new();

}
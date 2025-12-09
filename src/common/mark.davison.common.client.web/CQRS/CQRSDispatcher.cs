namespace mark.davison.common.client.web.CQRS;

public class CQRSDispatcher : ICQRSDispatcher
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly IActionDispatcher _actionDispatcher;

    public CQRSDispatcher(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, IActionDispatcher actionDispatcher)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _actionDispatcher = actionDispatcher;
    }

    Task<TCommandResult> ICommandDispatcher.Dispatch<TCommand, TCommandResult>(
        TCommand command,
        CancellationToken cancellation
    ) => _commandDispatcher.Dispatch<TCommand, TCommandResult>(command, cancellation);

    Task<TCommandResult> ICommandDispatcher.Dispatch<TCommand, TCommandResult>(
        CancellationToken cancellation
    ) => _commandDispatcher.Dispatch<TCommand, TCommandResult>(cancellation);

    Task<TQueryResult> IQueryDispatcher.Dispatch<TQuery, TQueryResult>(
        TQuery query,
        CancellationToken cancellation
    ) => _queryDispatcher.Dispatch<TQuery, TQueryResult>(query, cancellation);

    Task<TQueryResult> IQueryDispatcher.Dispatch<TQuery, TQueryResult>(
        CancellationToken cancellation
    ) => _queryDispatcher.Dispatch<TQuery, TQueryResult>(cancellation);

    Task IActionDispatcher.Dispatch<TAction>(
        TAction action,
        CancellationToken cancellation
    ) => _actionDispatcher.Dispatch<TAction>(action, cancellation);

    Task IActionDispatcher.Dispatch<TAction>(
        CancellationToken cancellation
    ) => _actionDispatcher.Dispatch<TAction>(cancellation);

    Task<TActionResponse> IActionDispatcher.Dispatch<TActionRequest, TActionResponse>(
        TActionRequest action,
        CancellationToken cancellation
    ) => _actionDispatcher.Dispatch<TActionRequest, TActionResponse>(action, cancellation);

    Task<TActionResponse> IActionDispatcher.Dispatch<TActionRequest, TActionResponse>(
        CancellationToken cancellation
    ) => _actionDispatcher.Dispatch<TActionRequest, TActionResponse>(cancellation);
}

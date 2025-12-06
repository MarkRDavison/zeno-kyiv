namespace mark.davison.common.client.abstractions.CQRS;

public interface IActionHandler<in TAction>
    where TAction : class, IAction<TAction>
{

    Task Handle(TAction action, CancellationToken cancellation);

}

public interface IResponseActionHandler<in TActionRequest, TActionResponse>
    where TActionRequest : class, IResponseAction<TActionRequest, TActionResponse>
    where TActionResponse : class
{

    Task<TActionResponse> Handle(TActionRequest action, CancellationToken cancellation);

}
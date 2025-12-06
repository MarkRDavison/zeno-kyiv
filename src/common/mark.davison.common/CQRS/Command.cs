namespace mark.davison.common.CQRS;

public interface ICommand<TCommand, TResponse>
    where TCommand : class
    where TResponse : class, new()
{
}
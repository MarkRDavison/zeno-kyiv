namespace mark.davison.common.CQRS;

public interface IQuery<TQuery, TResponse>
    where TQuery : class
    where TResponse : class, new()
{
}
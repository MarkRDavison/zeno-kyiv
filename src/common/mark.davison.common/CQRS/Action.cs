namespace mark.davison.common.CQRS;

public interface IAction<TRequest> where TRequest : class
{
}

public interface IResponseAction<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
}

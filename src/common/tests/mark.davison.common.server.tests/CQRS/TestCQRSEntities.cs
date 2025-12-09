namespace mark.davison.common.server.tests.CQRS;

public class ExampleCommandRequest : ICommand<ExampleCommandRequest, ExampleCommandResponse>
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class ExampleCommandResponse : Response
{

}

public class ExampleCommandHandler : ICommandHandler<ExampleCommandRequest, ExampleCommandResponse>
{
    public Task<ExampleCommandResponse> Handle(ExampleCommandRequest command, ICurrentUserContext currentUserContext, CancellationToken cancellation)
    {
        return Task.FromResult(new ExampleCommandResponse());
    }
}

public class ExampleQueryRequest : IQuery<ExampleQueryRequest, ExampleQueryResponse>
{
    public string String { get; set; } = string.Empty;
    public Guid Guid { get; set; }
    public long Long { get; set; }
    public int Int { get; set; }
    public bool Bool { get; set; }
    public DateOnly DateOnly { get; set; }
    public object? Invalid { get; set; }
}

public class ExampleQueryResponse : Response
{

}

public class ExampleQueryHandler : IQueryHandler<ExampleQueryRequest, ExampleQueryResponse>
{
    public Task<ExampleQueryResponse> Handle(ExampleQueryRequest query, ICurrentUserContext currentUserContext, CancellationToken cancellation)
    {
        return Task.FromResult(new ExampleQueryResponse());
    }
}

public interface IListStateItem
{

}

public class ListStateItem : IListStateItem
{

}

public class ListStateCommandResponse<TListStateItem> : Response<List<TListStateItem>> { }
public class ListStateCommandRequest<TListStateItem> : ICommand<ListStateCommandRequest<TListStateItem>, ListStateCommandResponse<TListStateItem>> { }

public abstract class ListStateCommandHandler<TListStateItem> : ICommandHandler<ListStateCommandRequest<TListStateItem>, ListStateCommandResponse<TListStateItem>>
{
    public Task<ListStateCommandResponse<TListStateItem>> Handle(ListStateCommandRequest<TListStateItem> command, ICurrentUserContext currentUserContext, CancellationToken cancellation)
    {
        return Task.FromResult(new ListStateCommandResponse<TListStateItem>());
    }
}

public class ListStateCommandHandlerImplementation : ListStateCommandHandler<ListStateItem>
{

}

public class ListStateQueryResponse<TListStateItem> : Response<List<TListStateItem>> { }
public class ListStateQueryRequest<TListStateItem> : IQuery<ListStateQueryRequest<TListStateItem>, ListStateQueryResponse<TListStateItem>> { }

public abstract class ListStateQueryHandler<TListStateItem> : IQueryHandler<ListStateQueryRequest<TListStateItem>, ListStateQueryResponse<TListStateItem>>
{
    public Task<ListStateQueryResponse<TListStateItem>> Handle(ListStateQueryRequest<TListStateItem> query, ICurrentUserContext currentUserContext, CancellationToken cancellation)
    {
        return Task.FromResult(new ListStateQueryResponse<TListStateItem>());
    }
}

public class ListStateQueryHandlerImplementation : ListStateQueryHandler<ListStateItem>
{

}
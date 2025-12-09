namespace mark.davison.common.client.abstractions.Store;

public class BaseActionResponse : Response
{
    public Guid ActionId { get; set; }

    public static TReturn From<TReturn>(TReturn response) where TReturn : BaseActionResponse, new()
    {
        return new TReturn()
        {
            ActionId = response.ActionId,
            Errors = response.Errors,
            Warnings = response.Warnings
        };
    }
}

public class BaseActionResponse<T> : BaseActionResponse
{
    public BaseActionResponse()
    {

    }
    public BaseActionResponse(BaseAction action)
    {
        ActionId = action.ActionId;
    }

    public static TReturn FromValue<TReturn>(TReturn response) where TReturn : BaseActionResponse<T>, new()
    {
        return new TReturn()
        {
            ActionId = response.ActionId,
            Errors = response.Errors,
            Warnings = response.Warnings,
            Value = response.Value
        };
    }

    [MemberNotNullWhen(returnValue: true, nameof(Response<T>.Value))]
    public bool SuccessWithValue => Success && Value != null;
    public T? Value { get; set; }
}

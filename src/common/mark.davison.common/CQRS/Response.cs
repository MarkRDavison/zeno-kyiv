namespace mark.davison.common.CQRS;

public class Response
{
    public bool Success => Errors.Count is 0;
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class Response<T> : Response
{
    [MemberNotNullWhen(returnValue: true, nameof(Value))]
    public bool SuccessWithValue => Success && Value is not null;
    public T? Value { get; set; }
}
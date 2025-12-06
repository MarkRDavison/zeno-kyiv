namespace mark.davison.common.CQRS;

public class Response
{
    public bool Success => !Errors.Any();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class Response<T> : Response
{
    [MemberNotNullWhen(returnValue: true, nameof(Response<>.Value))]
    public bool SuccessWithValue => Success && Value != null;
    public T? Value { get; set; }
}
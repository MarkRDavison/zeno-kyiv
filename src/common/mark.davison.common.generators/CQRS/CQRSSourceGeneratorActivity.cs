namespace mark.davison.common.generators.CQRS;

public enum CQRSActivityType
{
    Command,
    Query,
    Action,
    ResponseAction
}

public class CQRSSourceGeneratorActivity
{
    public CQRSSourceGeneratorActivity(
        bool isRequestDefinition,
        CQRSActivityType? type,
        string request,
        string response,
        string? handler,
        string? validator,
        string? processor,
        string? endpoint,
        string? rootNamespace,
        bool allowAnonymous,
        List<string> requiredRoles)
    {
        IsRequestDefinition = isRequestDefinition;
        Type = type;
        Request = request;
        Response = response;
        Handler = handler;
        Validator = validator;
        Processor = processor;
        Endpoint = endpoint;
        RootNamespace = rootNamespace;
        AllowAnonymous = allowAnonymous;
        RequiredRoles = requiredRoles;
    }

    public bool IsRequestDefinition { get; }
    public CQRSActivityType? Type { get; }
    public string Request { get; }
    public string Response { get; }
    public string? Handler { get; }
    public string? Validator { get; }
    public string? Processor { get; }
    public string? Endpoint { get; }
    public string? RootNamespace { get; }
    public bool AllowAnonymous { get; }
    public List<string> RequiredRoles { get; } = [];

    public string Key => $"{Type}_{Request}_{Response}_{RootNamespace}";
}

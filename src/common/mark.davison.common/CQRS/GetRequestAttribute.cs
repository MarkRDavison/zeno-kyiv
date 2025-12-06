namespace mark.davison.common.CQRS;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class GetRequestAttribute : Attribute
{
    public required string Path { get; set; }
    public string[] RequireRoles { get; set; } = [];
    public bool AllowAnonymous { get; set; }
}

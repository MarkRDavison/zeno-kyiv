namespace mark.davison.common.CQRS;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PostRequestAttribute : Attribute
{
    public string Path { get; set; } = string.Empty;
    public string[] RequireRoles { get; set; } = [];
    public bool AllowAnonymous { get; set; }
}
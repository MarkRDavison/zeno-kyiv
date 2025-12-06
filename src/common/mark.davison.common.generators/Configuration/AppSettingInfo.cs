namespace mark.davison.common.generators.Configuration;

public sealed class AppSettingInfo
{
    public bool IsRoot { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FullyQualifiedName => $"{Namespace}.{Name}";
    public List<AppSettingInfo> Children { get; set; } = [];
}

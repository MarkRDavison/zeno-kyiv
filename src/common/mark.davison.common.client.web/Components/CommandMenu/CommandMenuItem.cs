namespace mark.davison.common.client.web.Components;

public class CommandMenuItem
{
    public string Text { get; set; } = string.Empty;
    public string? Id { get; set; }
    public bool Disabled { get; set; }
    public bool Divider { get; set; }
    public List<CommandMenuItem>? Children { get; set; }
}
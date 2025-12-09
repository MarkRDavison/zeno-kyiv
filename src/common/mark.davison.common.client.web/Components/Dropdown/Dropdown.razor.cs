namespace mark.davison.common.client.web.Components;

public partial class Dropdown
{
    [Parameter]
    public string? Id { get; set; }

    [Parameter, EditorRequired]
    public required IEnumerable<IDropdownItem> Items { get; set; }

    [Parameter]
    public Guid? Value { get; set; }

    [Parameter]
    public EventCallback<Guid?> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<Guid?>>? For { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public string Label { get; set; } = string.Empty;
}

namespace mark.davison.common.client.web.Components;

public partial class CommandMenu
{
    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public Origin AnchorOrigin { get; set; } = Origin.BottomLeft;

    [Parameter, EditorRequired]
    public required List<CommandMenuItem> Items { get; set; }

    private Task OnActivate(CommandMenuItem item) => OnCommandMenuItemSelected.InvokeAsync(item);

    [Parameter]
    public EventCallback<CommandMenuItem> OnCommandMenuItemSelected { get; set; }
}

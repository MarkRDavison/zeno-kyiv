namespace mark.davison.common.client.web.Components;

public partial class ProgressButton
{
    [Parameter, EditorRequired]
    public required bool InProgress { get; set; }

    [Parameter, EditorRequired]
    public required bool Disabled { get; set; }

    [Parameter, EditorRequired]
    public required string Label { get; set; }

    [Parameter]
    public string? StartIcon { get; set; }

    [Parameter]
    public MudBlazor.Color Color { get; set; } = MudBlazor.Color.Success;

    [Parameter]
    public EventCallback OnClick { get; set; }
}

namespace mark.davison.common.client.web.Components;

public partial class NumericField
{
    [Parameter]
    public int? DecimalPlaces { get; set; }

    [Parameter]
    public bool AllowNegative { get; set; } = true;

    protected override void OnParametersSet()
    {
        if (DecimalPlaces == null)
        {
            Format = null;
        }
        else
        {
            Format = $"N{DecimalPlaces.Value}";
        }

        Min = AllowNegative ? null : 0;
    }
}

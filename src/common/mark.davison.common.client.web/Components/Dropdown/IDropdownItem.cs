namespace mark.davison.common.client.web.Components;

public interface IDropdownItem
{
    Guid Id { get; }
    string Name { get; }
}

public class DropdownItem : IDropdownItem
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }
}

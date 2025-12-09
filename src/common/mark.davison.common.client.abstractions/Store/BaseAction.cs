namespace mark.davison.common.client.abstractions.Store;

public class BaseAction
{
    public Guid ActionId { get; set; } = Guid.NewGuid();
}

namespace mark.davison.common.server.Notifications.Matrix.Client;

public class MatrixIdentifier
{
    public MatrixIdentifier(UserIdentifier user)
    {
        Type = "m.id.user";
        User = user.Name;
    }

    public string Type { get; }
    public string? User { get; }
}
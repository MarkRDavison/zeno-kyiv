namespace mark.davison.common.server.Notifications.Matrix.Client;

public sealed record PasswordLoginBody(string Username, string Password, string Session);
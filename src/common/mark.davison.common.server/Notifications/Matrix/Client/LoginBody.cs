namespace mark.davison.common.server.Notifications.Matrix.Client;

public class LoginBody
{
    public LoginBody(PasswordLoginBody passwordBody)
    {
        Type = "m.login.password";
        Identifier = new(new UserIdentifier(passwordBody.Username));
        Password = passwordBody.Password;
        Session = passwordBody.Session;
    }

    public string Type { get; }
    public MatrixIdentifier Identifier { get; }
    public string? Password { get; }
    public string Session { get; }
}
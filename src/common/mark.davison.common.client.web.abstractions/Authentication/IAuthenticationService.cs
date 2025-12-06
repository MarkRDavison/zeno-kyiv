namespace mark.davison.common.client.web.abstractions.Authentication;

public interface IAuthenticationService
{
    Task EvaluateAuthentication();
    void AuthenticateUser(ClaimsPrincipal user);

    event EventHandler<ClaimsPrincipal> UserChanged;
}

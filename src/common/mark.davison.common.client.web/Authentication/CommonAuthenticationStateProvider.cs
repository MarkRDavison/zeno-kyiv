namespace mark.davison.common.client.web.Authentication;

public sealed class CommonAuthenticationStateProvider : AuthenticationStateProvider
{
    private AuthenticationState _authenticationState;

    public CommonAuthenticationStateProvider(IAuthenticationService authenticationService)
    {
        _authenticationState = new AuthenticationState(new ClaimsPrincipal());

        authenticationService.UserChanged += (_, newUser) =>
        {
            _authenticationState = new AuthenticationState(newUser);
            NotifyAuthenticationStateChanged(Task.FromResult(_authenticationState));
        };
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_authenticationState);
}

namespace mark.davison.common.client.web.Authentication;

public sealed class CommonAuthenticationStateProvider : AuthenticationStateProvider
{
    bool _firstAuthDone = false;
    private AuthenticationState _authenticationState;
    private readonly IAuthenticationService _authenticationService;

    public CommonAuthenticationStateProvider(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        _authenticationState = new AuthenticationState(new ClaimsPrincipal());

        _authenticationService.UserChanged += (_, newUser) =>
        {
            _firstAuthDone = true;
            _authenticationState = new AuthenticationState(newUser);
            NotifyAuthenticationStateChanged(Task.FromResult(_authenticationState));
        };
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_firstAuthDone)
        {
            await _authenticationService.EvaluateAuthentication();
        }
        return _authenticationState;
    }
}

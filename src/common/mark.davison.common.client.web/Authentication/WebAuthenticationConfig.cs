namespace mark.davison.common.client.web.Authentication;

public sealed class WebAuthenticationConfig : IWebAuthenticationConfig
{
    public string HttpClientName { get; set; } = string.Empty;
    public string RemoteBase { get; private set; } = string.Empty;
    public string LoginEndpoint => RemoteBase + "/auth/login";
    public string LogoutEndpoint => RemoteBase + "/auth/logout";
    public string UserEndpoint => RemoteBase + "/auth/user";

    public void SetRemoteBase(string remoteBase)
    {
        RemoteBase = remoteBase.TrimEnd('/');
    }
}

namespace mark.davison.common.client.web.abstractions.Authentication;

public interface IWebAuthenticationConfig
{
    string LoginEndpoint { get; }
    string LogoutEndpoint { get; }
    string UserEndpoint { get; }
    string HttpClientName { get; set; }
    string RemoteBase { get; }
    void SetRemoteBase(string remoteBase);
}
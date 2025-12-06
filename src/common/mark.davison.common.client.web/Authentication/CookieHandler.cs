namespace mark.davison.common.client.web.Authentication;

public class CookieHandler : DelegatingHandler
{
    public CookieHandler()
    {
    }
    public CookieHandler(HttpMessageHandler handler)
    {
        InnerHandler = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        return await base.SendAsync(request, cancellationToken);
    }
}
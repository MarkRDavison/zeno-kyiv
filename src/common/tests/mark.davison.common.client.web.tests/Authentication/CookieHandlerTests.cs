namespace mark.davison.common.client.web.tests.Authentication;

public sealed class CookieHandlerTests
{
    [Test]
    public async Task SendAsync_SetsCredentialsToInclude()
    {
        await Task.CompletedTask;

        var cookieHandler = new CookieHandler
        {
            InnerHandler = new TestHandler()
        };

        var request = new HttpRequestMessage();
        var invoker = new HttpMessageInvoker(cookieHandler);
        await invoker.SendAsync(request, new CancellationToken());

        var val = request.Options.FirstOrDefault().Value as Dictionary<string, object>;
        await Assert.That(val).IsNotNull();
        await Assert.That(val).Contains(kv => kv.Key == "credentials" && kv.Value as string == "include");
    }
}

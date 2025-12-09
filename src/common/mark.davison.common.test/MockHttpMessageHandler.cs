namespace mark.davison.common.test;

[ExcludeFromCodeCoverage]
public class MockHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, Task<HttpResponseMessage>>? SendAsyncFunc { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (SendAsyncFunc is not null)
        {
            return await SendAsyncFunc(request);
        }

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}

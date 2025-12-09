namespace mark.davison.common.server.test.Framework;

public class StubAsyncDisposable : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
    }
}

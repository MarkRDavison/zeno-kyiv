namespace mark.davison.common.server.test.Framework;

public interface ICommonWebApplicationFactory<TSettings>
{
    public HttpClient CreateClient();
    public IServiceProvider ServiceProvider { get; }
}
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Logging
    .AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

builder.Services
    .AddScoped(sp => new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    })
    .AddExampleComponents();

await builder.Build().RunAsync();

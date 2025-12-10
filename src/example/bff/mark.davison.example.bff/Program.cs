namespace mark.davison.example.bff;

public sealed class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls(urls: Environment.GetEnvironmentVariable("EXAMPLE__URL") ?? "https://0.0.0.0:40000");
            })
            .ConfigureAppConfiguration((hostingContext, configurationBuilder) =>
            {
                configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                configurationBuilder.AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true);
                configurationBuilder.AddEnvironmentVariables();
            });
}
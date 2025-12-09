namespace mark.davison.common.server.Middleware;

[ExcludeFromCodeCoverage]
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        if (!context.Request.Path.ToString().Contains("health"))
        {
            Console.WriteLine("========== REQ START ==========");
            Console.WriteLine("REQUEST: {0} {1}", context.Request.Method, context.Request.Path);
        }

        await _next.Invoke(context);

        if (!context.Request.Path.ToString().Contains("health"))
        {
            Console.WriteLine("RESPONSE: {0}", context.Response.StatusCode);
            Console.WriteLine("========== REQ END ==========");
        }
    }

}
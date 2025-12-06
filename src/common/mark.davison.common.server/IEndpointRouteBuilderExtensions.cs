using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace mark.davison.common.server;

public static class IEndpointRouteBuilderExtensions
{

    public static IEndpointRouteBuilder UseApiProxy(
        this IEndpointRouteBuilder endpoints,
        string apiEndpoint)
    {
        endpoints.Map("/api/{**catchall}", async (
            HttpContext context,
            [FromServices] IHttpClientFactory httpClientFactory,
            CancellationToken cancellationToken) =>
        {
            var access_token = string.Empty; // TODO:

            //if (string.IsNullOrEmpty(access_token))
            //{
            //    return Results.Unauthorized();
            //}
            //
            var client = httpClientFactory.CreateClient("ApiProxy");
            var request = new HttpRequestMessage(
                new HttpMethod(context.Request.Method),
                $"{apiEndpoint.TrimEnd('/')}{context.Request.Path}{context.Request.QueryString}")
            {
                Content = new StreamContent(context.Request.Body)
            };

            // TODO: Construct auth header properly
            //request.Headers.TryAddWithoutValidation(HeaderNames.Authorization, $"Bearer {access_token}");

            var response = await client.SendAsync(request, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Results.Text(content);
            }

            return Results.BadRequest(new Response
            {
                Errors = ["BAD_REQUEST", $"{response.StatusCode}", content]
            });
        })
        .RequireAuthorization(); // TODO: Not always required???

        return endpoints;
    }

}

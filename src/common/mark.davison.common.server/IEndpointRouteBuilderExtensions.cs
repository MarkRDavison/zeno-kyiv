using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Net.Http.Headers;

namespace mark.davison.common.server;

public static class IEndpointRouteBuilderExtensions
{

    public static IEndpointRouteBuilder UseApiProxy(
        this IEndpointRouteBuilder endpoints,
        string apiEndpoint)
    {
        // TODO: Try use YARP for this
        endpoints.Map("/api/{**catchall}", async (
            HttpContext context,
            [FromServices] IHttpClientFactory httpClientFactory,
            CancellationToken cancellationToken) =>
        {

            var token = await context.GetTokenAsync("id_token");
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

            // TODO: Only send this if the endpoint requires auth?
            // I.e. for CQRS that allows anonymous dont send it?
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

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
        .RequireAuthorization(); // TODO: Not always required??? link to cqrs AllowAnonymous

        return endpoints;
    }

}

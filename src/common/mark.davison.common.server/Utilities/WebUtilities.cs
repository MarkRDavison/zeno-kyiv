namespace mark.davison.common.server.Utilities;


public static class WebUtilities
{
    public static class ContentType
    {
        public const string Json = "application/json";
        public const string FormUrlEncoded = "application/x-www-form-urlencoded";
    }

    public static Uri CreateQueryUri(string uri, IDictionary<string, string> queryParams)
    {
        var encodedQueryStringParams = queryParams.Select(p => string.Format("{0}={1}", p.Key, HttpUtility.UrlEncode(p.Value)));
        var url = new UriBuilder(uri);
        url.Query = string.Join("&", encodedQueryStringParams);

        return url.Uri;
    }

    public static RedirectHttpResult RedirectPreserveMethod(string uri)
    {
        return (RedirectHttpResult)Results.Redirect(uri, false, true);
    }

    public static string CreateBearerHeaderValue(string token) => "Bearer " + token;

    public static async Task<string> GetRequestBody(HttpRequest request)
    {
        try
        {
            if (request.ContentLength == 0)
            {
                return string.Empty;
            }

            using var bodyStream = new StreamReader(request.Body);

            return await bodyStream.ReadToEndAsync();
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public static async Task<TRequest> GetRequestFromBody<TRequest, TResponse>(HttpRequest httpRequest)
        where TRequest : class, ICommand<TRequest, TResponse>, new()
        where TResponse : class, new()
    {
        var bodyText = await GetRequestBody(httpRequest);

        if (string.IsNullOrEmpty(bodyText))
        {
            return new TRequest();
        }

        var request = JsonSerializer.Deserialize<TRequest>(bodyText, SerializationHelpers.CreateStandardSerializationOptions());

        if (request == null)
        {
            return new TRequest();
        }

        return request;
    }

    public static TRequest GetRequestFromQuery<TRequest, TResponse>(HttpRequest httpRequest)
        where TRequest : class, IQuery<TRequest, TResponse>, new()
        where TResponse : class, new()
    {

        var requestProperties = typeof(TRequest)
            .GetProperties()
            .Where(_ => _.CanWrite)
            .ToList();

        var request = new TRequest();

        var queryDictionary = httpRequest.Query.ToDictionary(_ => _.Key.ToLowerInvariant(), _ => _.Value.ToString());
        foreach (var property in requestProperties.Where(_ => queryDictionary.ContainsKey(_.Name.ToLowerInvariant())))
        {
            // TODO: Dont like this... source generator or some better serialize/deserialize loop
            var queryProperty = queryDictionary[property.Name.ToLowerInvariant()];

            if (property.PropertyType == typeof(string))
            {
                property.SetValue(request, queryProperty);
            }
            else if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
            {
                if (Guid.TryParse(queryProperty, out var guid))
                {
                    property.SetValue(request, guid);
                }
                else
                {
                    throw new InvalidOperationException("Invalid Guid format");
                }
            }
            else if (property.PropertyType == typeof(long) || property.PropertyType == typeof(long?))
            {
                if (long.TryParse(queryProperty, out var lnum))
                {
                    property.SetValue(request, lnum);
                }
                else
                {
                    throw new InvalidOperationException("Invalid long format");
                }
            }
            else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
            {
                if (int.TryParse(queryProperty, out var lnum))
                {
                    property.SetValue(request, lnum);
                }
                else
                {
                    throw new InvalidOperationException("Invalid int format");
                }
            }
            else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                if (bool.TryParse(queryProperty, out var bval))
                {
                    property.SetValue(request, bval);
                }
                else
                {
                    throw new InvalidOperationException("Invalid bool format");
                }
            }
            else if (property.PropertyType == typeof(DateOnly) || property.PropertyType == typeof(DateOnly))
            {
                if (DateOnly.TryParse(queryProperty, out var dval))
                {
                    property.SetValue(request, dval);
                }
                else
                {
                    throw new InvalidOperationException("Invalid DateOnly format");
                }
            }
            else
            {
                throw new InvalidOperationException($"Unhandled get property type '{property.PropertyType.Name}'");
            }
        }

        return request;
    }
}
namespace mark.davison.common.Repository;

// TODO: Is this needed?  Just need cqrs query -> query string
public class QueryParameters : Dictionary<string, string>
{
    public static readonly string[] BodyParameters = { "where", "project" };

    public string CreateQueryString()
    {
        string uri = string.Empty;
        if (this.Any())
        {
            uri += "?";
            uri += string.Join("&", this.Where(_ => !BodyParameters.Contains(_.Key)).Select((kv) => $"{kv.Key.ToLowerInvariant()}={HttpUtility.UrlEncode(kv.Value)}"));
        }
        return uri;
    }

    public string CreateBody()
    {
        var bodyParemeters = this.Where(_ => BodyParameters.Contains(_.Key)).ToList();

        if (bodyParemeters.Any())
        {
            var body = new JsonObject();

            foreach (var p in bodyParemeters)
            {
                body.Add(p.Key, JsonValue.Create(p.Value));
            }

            return JsonSerializer.Serialize(body);
        }

        return string.Empty;
    }


    public void Include(string path)
    {
        if (!ContainsKey("include"))
        {
            this["include"] = path;
        }
        else
        {
            this["include"] += "|" + path;
        }
    }
}
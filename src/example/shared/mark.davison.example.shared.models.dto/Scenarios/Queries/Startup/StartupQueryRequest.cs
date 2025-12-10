namespace mark.davison.example.shared.models.dto.Scenarios.Queries.Startup;

[GetRequest(Path = "startup-query", AllowAnonymous = true)]
public sealed class StartupQueryRequest : IQuery<StartupQueryRequest, StartupQueryResponse>;
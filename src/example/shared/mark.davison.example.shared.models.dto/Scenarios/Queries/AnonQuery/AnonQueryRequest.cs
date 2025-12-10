namespace mark.davison.example.shared.models.dto.Scenarios.Queries.AnonQuery;

[GetRequest(Path = "anon-query", AllowAnonymous = true)]
public sealed class AnonQueryRequest : IQuery<AnonQueryRequest, AnonQueryResponse>
{
}

namespace mark.davison.example.shared.models.dto.Scenarios.Queries.AdminSettings;

[GetRequest(Path = "admin-settings", RequireRoles = ["Admin"])]
public sealed class AdminSettingsQueryRequest : IQuery<AdminSettingsQueryRequest, AdminSettingsQueryResponse>;

using mark.davison.example.shared.models.dto.Scenarios.Queries.AdminSettings;

namespace mark.davison.example.queries.Scenarios.AdminSettings;

public sealed class AdminSettingsQueryProcessor : IQueryProcessor<AdminSettingsQueryRequest, AdminSettingsQueryResponse>
{
    private readonly IDbContext _dbContext;

    public AdminSettingsQueryProcessor(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminSettingsQueryResponse> ProcessAsync(AdminSettingsQueryRequest request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new AdminSettingsQueryResponse
        {
            Value = new AdminSettingsDto()
        };
    }
}

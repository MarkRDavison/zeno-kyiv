namespace mark.davison.common.persistence.Helpers;

public interface IDataSeeder
{
    Task SeedDataAsync(CancellationToken token);
}

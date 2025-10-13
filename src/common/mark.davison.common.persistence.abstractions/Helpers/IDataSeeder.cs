namespace mark.davison.common.persistence.abstractions.Helpers;

public interface IDataSeeder
{
    Task SeedDataAsync(CancellationToken token);
}

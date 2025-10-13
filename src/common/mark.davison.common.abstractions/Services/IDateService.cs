namespace mark.davison.common.abstractions.Services;

public interface IDateService
{
    public DateTime Now { get; }
    public DateOnly Today { get; }
}

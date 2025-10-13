namespace mark.davison.common.Services;

public class DateService : IDateService
{
    public enum DateMode
    {
        Utc,
        Local
    }

    private readonly DateMode _mode;

    public DateService(DateMode mode)
    {
        _mode = mode;
    }

    public DateTime Now => _mode == DateMode.Utc ? DateTime.UtcNow : DateTime.Now;
    public DateOnly Today => DateOnly.FromDateTime(Now);

}
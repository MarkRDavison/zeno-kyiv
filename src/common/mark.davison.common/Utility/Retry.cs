namespace mark.davison.common.Utility;

public static class Retry
{
    public static async Task Do(
        Func<CancellationToken, Task> action,
        TimeSpan retryInterval,
        CancellationToken cancellationToken,
        int maxAttemptCount = 3)
    {
        var exceptions = new List<Exception>();

        for (int attempted = 0; attempted < maxAttemptCount; attempted++)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (attempted > 0)
                {
                    await Task.Delay(retryInterval);
                }

                await action(cancellationToken);

                return;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        throw new AggregateException(exceptions);
    }

    public static async Task<T?> Do<T>(
        Func<CancellationToken, Task<T>> action,
        TimeSpan retryInterval,
        CancellationToken cancellationToken,
        int maxAttemptCount = 3)
    {
        var exceptions = new List<Exception>();

        for (int attempted = 0; attempted < maxAttemptCount; attempted++)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return default;
                }

                if (attempted > 0)
                {
                    await Task.Delay(retryInterval);
                }

                return await action(cancellationToken);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        throw new AggregateException(exceptions);
    }
}
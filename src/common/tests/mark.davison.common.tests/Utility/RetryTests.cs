using System.Diagnostics;

namespace mark.davison.common.tests.Utility;

public sealed class RetryTests
{
    [Test]
    public async Task Do_NoReturn_ReturnsIfActionSucceeds()
    {
        await Retry.Do(_ => Task.CompletedTask, TimeSpan.FromMicroseconds(1), CancellationToken.None);
    }

    [Test]
    public async Task Do_WithReturns_ReturnsIfActionSucceeds()
    {
        var val = 1;
        var result = await Retry.Do(
            _ => Task.FromResult(val),
            TimeSpan.FromMicroseconds(1),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(val);
    }

    [Test]
    public async Task Do_NoReturn_CallsActionUntilItSucceeds()
    {
        int maxTimes = 5;
        int timesCalled = 0;
        await Retry.Do(_ =>
        {
            timesCalled++;
            if (timesCalled == maxTimes)
            {
                return Task.CompletedTask;
            }
            throw new InvalidOperationException();
        }, TimeSpan.FromMicroseconds(1), CancellationToken.None, maxTimes);

        await Assert.That(timesCalled).IsEqualTo(maxTimes);
    }

    [Test]
    public async Task Do_WithReturns_CallsActionUntilItSucceeds()
    {
        int maxTimes = 5;
        int timesCalled = 0;

        var val = 1;
        var result = await Retry.Do(_ =>
        {
            timesCalled++;
            if (timesCalled == maxTimes)
            {
                return Task.FromResult(val);
            }
            throw new InvalidOperationException();
        }, TimeSpan.FromMicroseconds(1), CancellationToken.None, maxTimes);

        await Assert.That(result).IsEqualTo(val);
        await Assert.That(timesCalled).IsEqualTo(maxTimes);
    }

    [Test]
    public async Task Do_NoReturn_DoesNotCallActionIfCancellationRequested()
    {
        CancellationTokenSource cts = new();
        cts.Cancel();
        int timesCalled = 0;
        await Retry.Do(_ =>
        {
            timesCalled++;
            return Task.CompletedTask;

        }, TimeSpan.FromMicroseconds(1), cts.Token);

        await Assert.That(timesCalled).IsEqualTo(0);
    }

    [Test]
    public async Task Do_WithReturns_DoesNotCallActionIfCancellationRequested()
    {
        CancellationTokenSource cts = new();
        cts.Cancel();
        int timesCalled = 0;
        await Retry.Do(_ =>
        {
            timesCalled++;
            return Task.FromResult(1);

        }, TimeSpan.FromMicroseconds(1), cts.Token);

        await Assert.That(timesCalled).IsEqualTo(0);
    }

    [Test]
    public async Task Do_NoReturn_DelaysUsingTheRetryInterval()
    {
        int maxTimes = 5;
        int timesCalled = 0;
        var delay = TimeSpan.FromMilliseconds(5);

        Stopwatch sw = new();

        sw.Start();
        await Retry.Do(_ =>
        {
            timesCalled++;
            if (timesCalled == maxTimes)
            {
                return Task.CompletedTask;
            }
            throw new InvalidOperationException();
        }, delay, CancellationToken.None, maxTimes);

        await Assert.That(sw.Elapsed).IsGreaterThan(delay * (maxTimes - 1));
    }

    [Test]
    public async Task Do_WithReturns_DelaysUsingTheRetryInterval()
    {
        int maxTimes = 5;
        int timesCalled = 0;
        var delay = TimeSpan.FromMilliseconds(5);

        var val = 1;
        Stopwatch sw = new();

        sw.Start();
        var result = await Retry.Do(_ =>
        {
            timesCalled++;
            if (timesCalled == maxTimes)
            {
                return Task.FromResult(val);
            }
            throw new InvalidOperationException();
        }, delay, CancellationToken.None, maxTimes);

        await Assert.That(sw.Elapsed).IsGreaterThan(delay * (maxTimes - 1));
    }

    [Test]
    public async Task Do_NoReturn_ThrowsAggregateException_MaxRetriesReached()
    {
        await Assert.ThrowsAsync<AggregateException>(
            async () => await Retry.Do(_ =>
            {
                throw new InvalidOperationException();
            }, TimeSpan.FromMicroseconds(1), CancellationToken.None));
    }

    [Test]
    public async Task Do_WithReturns_ThrowsAggregateException_MaxRetriesReached()
    {
        await Assert.ThrowsAsync<AggregateException>(
            async () => await Retry.Do<int>(_ =>
        {
            throw new InvalidOperationException();
        }, TimeSpan.FromMicroseconds(1), CancellationToken.None));
    }
}

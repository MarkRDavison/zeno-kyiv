using mark.davison.common.Services;

namespace mark.davison.common.tests.Services;


public sealed class DateServiceTests
{
    [Test]
    public async Task Today_InLocalMode_ReturnsCorrectly()
    {
        DateService _dateService = new(DateService.DateMode.Local);

        var now = _dateService.Today;

        await Assert.That(now).IsEqualTo(DateOnly.FromDateTime(DateTime.Today));
    }

    [Test]
    public async Task Today_InUtcMode_ReturnsCorrectly()
    {
        DateService _dateService = new(DateService.DateMode.Utc);

        var now = _dateService.Today;

        await Assert.That(now).IsEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Test]
    public async Task Now_InLocalMode_ReturnsCorrectly()
    {
        var start = DateTime.Now;
        DateService _dateService = new(DateService.DateMode.Local);

        var now = _dateService.Now;

        await Assert.That(now).IsGreaterThanOrEqualTo(start);
        await Assert.That(now).IsLessThanOrEqualTo(DateTime.Now);
    }

    [Test]
    public async Task Now_InUtcMode_ReturnsCorrectly()
    {
        var start = DateTime.UtcNow;
        DateService _dateService = new(DateService.DateMode.Utc);

        var now = _dateService.Now;

        await Assert.That(now).IsGreaterThanOrEqualTo(start);
        await Assert.That(now).IsLessThanOrEqualTo(DateTime.UtcNow);
    }
}

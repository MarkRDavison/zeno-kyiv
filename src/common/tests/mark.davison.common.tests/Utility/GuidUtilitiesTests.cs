namespace mark.davison.common.tests.Utility;

public sealed class GuidUtilitiesTests
{
    [Test]
    public async Task CombineTwoGuids_ReturnsConsistantly()
    {
        Guid guid1 = Guid.NewGuid();
        Guid guid2 = Guid.NewGuid();

        await Assert
            .That(GuidUtilities.CombineTwoGuids(guid1, guid2))
            .IsEqualTo(GuidUtilities.CombineTwoGuids(guid1, guid2));
    }

    [Test]
    public async Task CombineTwoGuids_ReturnsDifferentToEitherInput()
    {
        Guid guid1 = Guid.NewGuid();
        Guid guid2 = Guid.NewGuid();

        var combined = GuidUtilities.CombineTwoGuids(guid1, guid2);

        await Assert
            .That(guid1)
            .IsNotEqualTo(combined);
        await Assert
            .That(guid2)
            .IsNotEqualTo(combined);
    }
}

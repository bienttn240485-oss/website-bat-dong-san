using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Tests.Domain;

public sealed class PropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WhenValid_CreatesPropertyAndNormalizesCode()
    {
        var property = CreateProperty(code: "  op-0101 ");

        Assert.Equal("OP-0101", property.Code);
        Assert.Equal(PropertyStatus.Available, property.Status);
        Assert.Equal(Now, property.CreatedAtUtc);
        Assert.Equal(Now, property.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WhenCodeEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateProperty(code: " "));
    }

    [Fact]
    public void Constructor_WhenMonthlyPriceNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateProperty(monthlyPrice: -1));
    }

    [Fact]
    public void Constructor_WhenSalePriceNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateProperty(salePrice: -1));
    }

    [Fact]
    public void ChangeStatus_UpdatesStatusAndTimestamp()
    {
        var property = CreateProperty();
        var changedAt = Now.AddHours(1);

        property.ChangeStatus(PropertyStatus.Reserved, changedAt);

        Assert.Equal(PropertyStatus.Reserved, property.Status);
        Assert.Equal(changedAt, property.UpdatedAtUtc);
    }

    private static Property CreateProperty(
        string code = "OP-0101",
        long? monthlyPrice = 18_000_000,
        long? salePrice = 5_500_000_000)
        => new(
            Guid.NewGuid(),
            code,
            PropertyProject.OpusOne,
            "S1",
            PropertyType.TwoBedroomTwoBathrooms,
            68.5m,
            2,
            monthlyPrice,
            salePrice,
            "East",
            "Bank loan supported",
            "Pink book",
            "Full furniture",
            "River-view apartment",
            "https://example.com/video",
            PropertyStatus.Available,
            new DateOnly(2026, 8, 1),
            "Priority listing",
            Now);
}

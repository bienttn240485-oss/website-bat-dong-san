using RealEstateManagement.Domain.Contracts;

namespace RealEstateManagement.Tests.Domain;

public sealed class LandlordContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly SignedDate = new(2026, 7, 1);

    [Fact]
    public void Constructor_WhenValid_CreatesLandlordContract()
    {
        var contract = CreateContract();

        Assert.NotEqual(Guid.Empty, contract.PropertyId);
        Assert.Equal("Nguyen Van A", contract.LandlordName);
        Assert.Equal(18_000_000, contract.InputPrice);
        Assert.Equal(new DateOnly(2027, 7, 1), contract.ExpiryDate);
    }

    [Fact]
    public void Constructor_WhenExpiryDateOmitted_DefaultsToTwelveMonths()
    {
        var contract = CreateContract(expiryDate: null);

        Assert.Equal(SignedDate.AddMonths(12), contract.ExpiryDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Constructor_WhenPaymentDayInvalid_Throws(int paymentDay)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateContract(paymentDay: paymentDay));
    }

    private static LandlordContract CreateContract(DateOnly? expiryDate = null, int? paymentDay = 5)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Nguyen Van A",
            "PE-001",
            "Sale A",
            18_000_000,
            SignedDate,
            expiryDate,
            DepositStatus.Pending,
            paymentDay,
            "1-5",
            new DateOnly(2026, 8, 5),
            "Managed property",
            Now);
}

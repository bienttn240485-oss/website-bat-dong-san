using RealEstateManagement.Domain.Contracts;

namespace RealEstateManagement.Tests.Domain;

public sealed class TenantContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly SignedDate = new(2026, 7, 15);

    [Fact]
    public void Constructor_CalculatesExpiryDate()
    {
        var contract = CreateContract(termMonths: 6);

        Assert.Equal(SignedDate.AddMonths(6), contract.ExpiryDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenTermInvalid_Throws(int termMonths)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateContract(termMonths: termMonths));
    }

    [Theory]
    [InlineData(-1, 20_000_000)]
    [InlineData(18_000_000, -1)]
    public void Constructor_WhenRentOrDepositNegative_Throws(long rentalPrice, long depositAmount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateContract(rentalPrice: rentalPrice, depositAmount: depositAmount));
    }

    [Fact]
    public void ChangeStatus_UpdatesStatusAndTimestamp()
    {
        var contract = CreateContract();
        var changedAt = Now.AddHours(2);

        contract.ChangeStatus(ContractStatus.Cancelled, changedAt);

        Assert.Equal(ContractStatus.Cancelled, contract.Status);
        Assert.Equal(changedAt, contract.UpdatedAtUtc);
    }

    private static TenantContract CreateContract(
        long rentalPrice = 18_000_000,
        int termMonths = 12,
        long depositAmount = 36_000_000)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tran Thi B",
            "Manager A",
            rentalPrice,
            SignedDate,
            termMonths,
            depositAmount,
            null,
            "PE-002",
            "123456",
            ContractStatus.Active,
            "Long-term tenant",
            Now);
}

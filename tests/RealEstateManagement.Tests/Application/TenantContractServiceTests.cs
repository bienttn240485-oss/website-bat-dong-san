using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Contracts;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Tests.Application;

public sealed class TenantContractServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateTenantContractAsync_WhenValid_AddsContract()
    {
        var store = new InMemoryTenantContractStore();
        var property = CreateProperty();
        store.Properties.Add(property);
        var service = new TenantContractService(store, new FixedClock());

        var result = await service.CreateTenantContractAsync(ValidCommand(property.Id));

        Assert.True(result.Succeeded);
        var contract = Assert.Single(store.Contracts);
        Assert.Equal(property.Id, contract.PropertyId);
        Assert.Equal(new DateOnly(2027, 7, 1), contract.ExpiryDate);
    }

    [Fact]
    public async Task CreateTenantContractAsync_WhenActiveContractOverlaps_ReturnsFailure()
    {
        var store = new InMemoryTenantContractStore();
        var property = CreateProperty();
        store.Properties.Add(property);
        store.Contracts.Add(CreateContract(property.Id, new DateOnly(2026, 7, 1), 12, ContractStatus.Active));
        var service = new TenantContractService(store, new FixedClock());

        var result = await service.CreateTenantContractAsync(ValidCommand(property.Id) with { SignedDate = new DateOnly(2026, 8, 1), TermMonths = 6 });

        Assert.False(result.Succeeded);
        Assert.Contains("Căn hộ đã có hợp đồng khách thuê đang hiệu lực trong khoảng thời gian này.", result.Errors);
    }

    [Fact]
    public async Task CreateTenantContractAsync_WhenActive_SetsPropertyOccupied()
    {
        var store = new InMemoryTenantContractStore();
        var property = CreateProperty();
        store.Properties.Add(property);
        var service = new TenantContractService(store, new FixedClock());

        await service.CreateTenantContractAsync(ValidCommand(property.Id));

        Assert.Equal(PropertyStatus.Occupied, property.Status);
    }

    [Fact]
    public async Task CreateTenantContractAsync_WhenActiveExpiresSoon_SetsPropertySoonAvailable()
    {
        var store = new InMemoryTenantContractStore();
        var property = CreateProperty();
        store.Properties.Add(property);
        var service = new TenantContractService(store, new FixedClock());

        await service.CreateTenantContractAsync(ValidCommand(property.Id) with { SignedDate = new DateOnly(2026, 5, 15), TermMonths = 3 });

        Assert.Equal(PropertyStatus.SoonAvailable, property.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenEnded_SetsPropertyAvailable()
    {
        var store = new InMemoryTenantContractStore();
        var property = CreateProperty(status: PropertyStatus.Occupied);
        var contract = CreateContract(property.Id, new DateOnly(2026, 7, 1), 12, ContractStatus.Active);
        store.Properties.Add(property);
        store.Contracts.Add(contract);
        var service = new TenantContractService(store, new FixedClock());

        var result = await service.ChangeStatusAsync(new TenantContractStatusCommand(contract.Id, ContractStatus.Cancelled));

        Assert.True(result.Succeeded);
        Assert.Equal(PropertyStatus.Available, property.Status);
    }

    [Fact]
    public async Task CreateTenantContractAsync_WhenPropertyReserved_DoesNotOverwriteStatus()
    {
        var store = new InMemoryTenantContractStore();
        var property = CreateProperty(status: PropertyStatus.Reserved);
        store.Properties.Add(property);
        var service = new TenantContractService(store, new FixedClock());

        await service.CreateTenantContractAsync(ValidCommand(property.Id));

        Assert.Equal(PropertyStatus.Reserved, property.Status);
    }

    [Theory]
    [InlineData(0, "Thời hạn thuê phải lớn hơn 0 tháng.")]
    public async Task CreateTenantContractAsync_WhenTermMonthsInvalid_ReturnsFailure(int termMonths, string expected)
    {
        var store = new InMemoryTenantContractStore();
        var property = CreateProperty();
        store.Properties.Add(property);
        var service = new TenantContractService(store, new FixedClock());

        var result = await service.CreateTenantContractAsync(ValidCommand(property.Id) with { TermMonths = termMonths });

        Assert.False(result.Succeeded);
        Assert.Contains(expected, result.Errors);
    }

    [Fact]
    public async Task CreateTenantContractAsync_WhenRentalPriceNegative_ReturnsFailure()
    {
        var store = new InMemoryTenantContractStore();
        var property = CreateProperty();
        store.Properties.Add(property);
        var service = new TenantContractService(store, new FixedClock());

        var result = await service.CreateTenantContractAsync(ValidCommand(property.Id) with { RentalPrice = -1 });

        Assert.False(result.Succeeded);
        Assert.Contains("Giá thuê không được âm.", result.Errors);
    }

    [Fact]
    public async Task CreateTenantContractAsync_WhenDepositAmountNegative_ReturnsFailure()
    {
        var store = new InMemoryTenantContractStore();
        var property = CreateProperty();
        store.Properties.Add(property);
        var service = new TenantContractService(store, new FixedClock());

        var result = await service.CreateTenantContractAsync(ValidCommand(property.Id) with { DepositAmount = -1 });

        Assert.False(result.Succeeded);
        Assert.Contains("Tiền cọc không được âm.", result.Errors);
    }

    [Fact]
    public void ContractDisplay_WhenPriceInAndPriceOutDiffer_CalculatesMargin()
    {
        var margin = 24_000_000 - 18_000_000;

        Assert.Equal(6_000_000, margin);
        Assert.Equal(72_000_000, margin * 12);
    }

    private static TenantContractEditorCommand ValidCommand(Guid propertyId)
        => new(
            propertyId,
            "Tran Thi B",
            "Manager A",
            20_000_000,
            new DateOnly(2026, 7, 1),
            12,
            40_000_000,
            null,
            "PE-002",
            "123456",
            ContractStatus.Active,
            null);

    private static Property CreateProperty(PropertyStatus status = PropertyStatus.Available)
        => new(Guid.NewGuid(), "OP-0101", PropertyProject.OpusOne, "S1", PropertyType.TwoBedroom, 68.5m, 2, 18_000_000, null, null, null, null, null, null, null, status, null, null, Now);

    private static TenantContract CreateContract(Guid propertyId, DateOnly signedDate, int termMonths, ContractStatus status)
        => new(Guid.NewGuid(), propertyId, "Tran Thi B", null, 20_000_000, signedDate, termMonths, 40_000_000, null, null, null, status, null, Now);

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class InMemoryTenantContractStore : ITenantContractStore
    {
        public List<Property> Properties { get; } = [];
        public List<TenantContract> Contracts { get; } = [];

        public Task<IReadOnlyList<TenantContractDto>> ListTenantContractsAsync(ContractFilterQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<TenantContractDto>>(Contracts.Select(ToDto).ToArray());

        public Task<TenantContractDto?> GetTenantContractAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(ToDtoOrNull(Contracts.FirstOrDefault(contract => contract.Id == id)));

        public Task<IReadOnlyList<TenantContractDto>> ListTenantContractsForPropertyAsync(Guid propertyId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<TenantContractDto>>(Contracts.Where(contract => contract.PropertyId == propertyId).Select(ToDto).ToArray());

        public Task<TenantContract?> GetTenantContractForUpdateAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Contracts.FirstOrDefault(contract => contract.Id == id));

        public Task<Property?> GetPropertyForUpdateAsync(Guid propertyId, CancellationToken cancellationToken)
            => Task.FromResult(Properties.FirstOrDefault(property => property.Id == propertyId));

        public Task<IReadOnlyList<TenantContract>> ListActiveTenantContractsAsync(Guid propertyId, Guid? exceptContractId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<TenantContract>>(Contracts
                .Where(contract => contract.PropertyId == propertyId && contract.Id != exceptContractId && contract.Status == ContractStatus.Active)
                .ToArray());

        public Task AddTenantContractAsync(TenantContract contract, CancellationToken cancellationToken)
        {
            Contracts.Add(contract);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static TenantContractDto ToDto(TenantContract contract)
            => new(
                contract.Id,
                contract.PropertyId,
                "OP-0101",
                null,
                "S1",
                contract.TenantName,
                contract.ManagerName,
                contract.RentalPrice,
                contract.SignedDate,
                contract.TermMonths,
                contract.ExpiryDate,
                contract.DepositAmount,
                contract.DepositReturnDate,
                contract.PeCode,
                contract.PassCode,
                contract.Status,
                contract.Notes);

        private static TenantContractDto? ToDtoOrNull(TenantContract? contract)
            => contract is null ? null : ToDto(contract);
    }
}
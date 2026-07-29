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
            => Task.FromResult<IReadOnlyList<TenantContractDto>>([]);

        public Task<TenantContractDto?> GetTenantContractAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<TenantContractDto?>(null);

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
    }
}

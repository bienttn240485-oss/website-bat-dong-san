using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Contracts;
using RealEstateManagement.Domain.Contracts;

namespace RealEstateManagement.Tests.Application;

public sealed class LandlordContractServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateLandlordContractAsync_WhenValid_AddsContract()
    {
        var propertyId = Guid.NewGuid();
        var store = new InMemoryLandlordContractStore();
        store.PropertyIds.Add(propertyId);
        var service = new LandlordContractService(store, new FixedClock());

        var result = await service.CreateLandlordContractAsync(ValidCommand(propertyId));

        Assert.True(result.Succeeded);
        var contract = Assert.Single(store.Contracts);
        Assert.Equal(propertyId, contract.PropertyId);
        Assert.Equal(new DateOnly(2027, 7, 1), contract.ExpiryDate);
    }

    [Fact]
    public async Task CreateLandlordContractAsync_WhenPropertyAlreadyHasContract_ReturnsFailure()
    {
        var propertyId = Guid.NewGuid();
        var store = new InMemoryLandlordContractStore();
        store.PropertyIds.Add(propertyId);
        store.Contracts.Add(CreateContract(propertyId));
        var service = new LandlordContractService(store, new FixedClock());

        var result = await service.CreateLandlordContractAsync(ValidCommand(propertyId));

        Assert.False(result.Succeeded);
        Assert.Contains("Căn hộ này đã có hợp đồng chủ nhà.", result.Errors);
    }

    private static LandlordContractEditorCommand ValidCommand(Guid propertyId)
        => new(
            propertyId,
            "Nguyen Van A",
            "PE-001",
            "Sale A",
            18_000_000,
            new DateOnly(2026, 7, 1),
            null,
            DepositStatus.Pending,
            5,
            "1-5",
            null,
            null);

    private static LandlordContract CreateContract(Guid propertyId)
        => new(Guid.NewGuid(), propertyId, "Nguyen Van A", null, null, 18_000_000, new DateOnly(2026, 7, 1), null, DepositStatus.Pending, 5, null, null, null, Now);

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class InMemoryLandlordContractStore : ILandlordContractStore
    {
        public HashSet<Guid> PropertyIds { get; } = [];
        public List<LandlordContract> Contracts { get; } = [];

        public Task<IReadOnlyList<LandlordContractDto>> ListLandlordContractsAsync(ContractFilterQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<LandlordContractDto>>([]);

        public Task<LandlordContractDto?> GetLandlordContractAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<LandlordContractDto?>(null);

        public Task<LandlordContract?> GetLandlordContractForUpdateAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Contracts.FirstOrDefault(contract => contract.Id == id));

        public Task<bool> PropertyExistsAsync(Guid propertyId, CancellationToken cancellationToken)
            => Task.FromResult(PropertyIds.Contains(propertyId));

        public Task<bool> ContractExistsForPropertyAsync(Guid propertyId, Guid? exceptContractId, CancellationToken cancellationToken)
            => Task.FromResult(Contracts.Any(contract => contract.PropertyId == propertyId && contract.Id != exceptContractId));

        public Task AddLandlordContractAsync(LandlordContract contract, CancellationToken cancellationToken)
        {
            Contracts.Add(contract);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

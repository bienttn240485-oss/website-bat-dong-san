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

    [Fact]
    public async Task CreateLandlordContractAsync_WhenExpiryBeforeSignedDate_ReturnsFailure()
    {
        var propertyId = Guid.NewGuid();
        var store = new InMemoryLandlordContractStore();
        store.PropertyIds.Add(propertyId);
        var service = new LandlordContractService(store, new FixedClock());

        var result = await service.CreateLandlordContractAsync(ValidCommand(propertyId) with { ExpiryDate = new DateOnly(2026, 6, 30) });

        Assert.False(result.Succeeded);
        Assert.Contains("Ngày hết hạn phải sau ngày ký.", result.Errors);
    }

    [Fact]
    public async Task CreateLandlordContractAsync_WhenPaymentDayOutsideRange_ReturnsFailure()
    {
        var propertyId = Guid.NewGuid();
        var store = new InMemoryLandlordContractStore();
        store.PropertyIds.Add(propertyId);
        var service = new LandlordContractService(store, new FixedClock());

        var result = await service.CreateLandlordContractAsync(ValidCommand(propertyId) with { PaymentDay = 32 });

        Assert.False(result.Succeeded);
        Assert.Contains("Ngày thanh toán phải từ 1 đến 31.", result.Errors);
    }

    [Fact]
    public async Task DeleteLandlordContractAsync_WhenRequested_KeepsHistory()
    {
        var propertyId = Guid.NewGuid();
        var contract = CreateContract(propertyId);
        var store = new InMemoryLandlordContractStore();
        store.PropertyIds.Add(propertyId);
        store.Contracts.Add(contract);
        var service = new LandlordContractService(store, new FixedClock());

        var result = await service.DeleteLandlordContractAsync(contract.Id);

        Assert.False(result.Succeeded);
        Assert.Single(store.Contracts);
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
            => Task.FromResult<IReadOnlyList<LandlordContractDto>>(Contracts.Select(ToDto).ToArray());

        public Task<LandlordContractDto?> GetLandlordContractAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(ToDtoOrNull(Contracts.FirstOrDefault(contract => contract.Id == id)));

        public Task<LandlordContractDto?> GetLandlordContractForPropertyAsync(Guid propertyId, CancellationToken cancellationToken)
            => Task.FromResult(ToDtoOrNull(Contracts.FirstOrDefault(contract => contract.PropertyId == propertyId)));

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

        private static LandlordContractDto ToDto(LandlordContract contract)
            => new(
                contract.Id,
                contract.PropertyId,
                "OP-0101",
                null,
                "S1",
                contract.LandlordName,
                contract.PeCode,
                contract.SaleName,
                contract.InputPrice,
                contract.SignedDate,
                contract.ExpiryDate,
                contract.DepositStatus,
                contract.PaymentDay,
                contract.PaymentWindow,
                contract.NextDueDate,
                contract.Notes);

        private static LandlordContractDto? ToDtoOrNull(LandlordContract? contract)
            => contract is null ? null : ToDto(contract);
    }
}
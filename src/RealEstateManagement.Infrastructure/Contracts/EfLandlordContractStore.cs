using RealEstateManagement.Application.Contracts;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace RealEstateManagement.Infrastructure.Contracts;

public sealed class EfLandlordContractStore(ApplicationDbContext dbContext) : ILandlordContractStore
{
    public async Task<IReadOnlyList<LandlordContractDto>> ListLandlordContractsAsync(ContractFilterQuery query, CancellationToken cancellationToken)
    {
        var contracts = await ApplyFilter(dbContext.LandlordContracts.AsNoTracking(), query)
            .OrderBy(contract => contract.ExpiryDate)
            .ToListAsync(cancellationToken);

        return contracts.Select(ToDto).ToArray();
    }

    public async Task<LandlordContractDto?> GetLandlordContractAsync(Guid id, CancellationToken cancellationToken)
    {
        var contract = await dbContext.LandlordContracts.AsNoTracking().FirstOrDefaultAsync(contract => contract.Id == id, cancellationToken);
        return contract is null ? null : ToDto(contract);
    }

    public Task<LandlordContract?> GetLandlordContractForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.LandlordContracts.FirstOrDefaultAsync(contract => contract.Id == id, cancellationToken);

    public Task<bool> PropertyExistsAsync(Guid propertyId, CancellationToken cancellationToken)
        => dbContext.Properties.AnyAsync(property => property.Id == propertyId, cancellationToken);

    public Task<bool> ContractExistsForPropertyAsync(Guid propertyId, Guid? exceptContractId, CancellationToken cancellationToken)
        => dbContext.LandlordContracts.AnyAsync(
            contract => contract.PropertyId == propertyId && (exceptContractId == null || contract.Id != exceptContractId.Value),
            cancellationToken);

    public async Task AddLandlordContractAsync(LandlordContract contract, CancellationToken cancellationToken)
        => await dbContext.LandlordContracts.AddAsync(contract, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<LandlordContract> ApplyFilter(IQueryable<LandlordContract> queryable, ContractFilterQuery query)
    {
        if (query.PropertyId is not null)
        {
            queryable = queryable.Where(contract => contract.PropertyId == query.PropertyId);
        }

        if (query.ExpiringBefore is not null)
        {
            queryable = queryable.Where(contract => contract.ExpiryDate <= query.ExpiringBefore.Value);
        }

        return queryable;
    }

    private static LandlordContractDto ToDto(LandlordContract contract)
        => new(
            contract.Id,
            contract.PropertyId,
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
}

using RealEstateManagement.Application.Contracts;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Properties;
using RealEstateManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace RealEstateManagement.Infrastructure.Contracts;

public sealed class EfTenantContractStore(ApplicationDbContext dbContext) : ITenantContractStore
{
    public async Task<IReadOnlyList<TenantContractDto>> ListTenantContractsAsync(ContractFilterQuery query, CancellationToken cancellationToken)
    {
        var contracts = await ApplyFilter(dbContext.TenantContracts.AsNoTracking(), query)
            .OrderBy(contract => contract.ExpiryDate)
            .ToListAsync(cancellationToken);

        return contracts.Select(ToDto).ToArray();
    }

    public async Task<TenantContractDto?> GetTenantContractAsync(Guid id, CancellationToken cancellationToken)
    {
        var contract = await dbContext.TenantContracts.AsNoTracking().FirstOrDefaultAsync(contract => contract.Id == id, cancellationToken);
        return contract is null ? null : ToDto(contract);
    }

    public Task<TenantContract?> GetTenantContractForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.TenantContracts.FirstOrDefaultAsync(contract => contract.Id == id, cancellationToken);

    public Task<Property?> GetPropertyForUpdateAsync(Guid propertyId, CancellationToken cancellationToken)
        => dbContext.Properties.FirstOrDefaultAsync(property => property.Id == propertyId, cancellationToken);

    public async Task<IReadOnlyList<TenantContract>> ListActiveTenantContractsAsync(Guid propertyId, Guid? exceptContractId, CancellationToken cancellationToken)
        => await dbContext.TenantContracts
            .Where(contract => contract.PropertyId == propertyId
                && contract.Status == ContractStatus.Active
                && (exceptContractId == null || contract.Id != exceptContractId.Value))
            .ToListAsync(cancellationToken);

    public async Task AddTenantContractAsync(TenantContract contract, CancellationToken cancellationToken)
        => await dbContext.TenantContracts.AddAsync(contract, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<TenantContract> ApplyFilter(IQueryable<TenantContract> queryable, ContractFilterQuery query)
    {
        if (query.PropertyId is not null)
        {
            queryable = queryable.Where(contract => contract.PropertyId == query.PropertyId);
        }

        if (query.Status is not null)
        {
            queryable = queryable.Where(contract => contract.Status == query.Status);
        }

        if (query.ExpiringBefore is not null)
        {
            queryable = queryable.Where(contract => contract.ExpiryDate <= query.ExpiringBefore.Value);
        }

        return queryable;
    }

    private static TenantContractDto ToDto(TenantContract contract)
        => new(
            contract.Id,
            contract.PropertyId,
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
}

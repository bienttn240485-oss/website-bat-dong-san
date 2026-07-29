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
        var rows = dbContext.TenantContracts.AsNoTracking()
            .Join(dbContext.Properties.AsNoTracking(),
                contract => contract.PropertyId,
                property => property.Id,
                (contract, property) => new { Contract = contract, Property = property });

        if (query.PropertyId is not null)
        {
            rows = rows.Where(row => row.Contract.PropertyId == query.PropertyId);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            rows = rows.Where(row =>
                row.Contract.TenantName.Contains(keyword)
                || row.Property.Code.Contains(keyword)
                || row.Property.Area.Contains(keyword)
                || (row.Contract.PeCode != null && row.Contract.PeCode.Contains(keyword))
                || (row.Contract.ManagerName != null && row.Contract.ManagerName.Contains(keyword)));
        }

        if (query.Project is not null)
        {
            rows = rows.Where(row => row.Property.Project == query.Project);
        }

        if (!string.IsNullOrWhiteSpace(query.Area))
        {
            var area = query.Area.Trim();
            rows = rows.Where(row => row.Property.Area == area);
        }

        if (query.Status is not null)
        {
            rows = rows.Where(row => row.Contract.Status == query.Status);
        }

        if (query.ExpiringBefore is not null)
        {
            rows = rows.Where(row => row.Contract.ExpiryDate <= query.ExpiringBefore.Value);
        }

        if (query.ExpiredOnly && query.ExpiringBefore is not null)
        {
            rows = rows.Where(row => row.Contract.ExpiryDate < query.ExpiringBefore.Value);
        }

        return await rows
            .OrderBy(row => row.Contract.ExpiryDate)
            .ThenBy(row => row.Property.Code)
            .Select(row => new TenantContractDto(
                row.Contract.Id,
                row.Contract.PropertyId,
                row.Property.Code,
                row.Property.Project,
                row.Property.Area,
                row.Contract.TenantName,
                row.Contract.ManagerName,
                row.Contract.RentalPrice,
                row.Contract.SignedDate,
                row.Contract.TermMonths,
                row.Contract.ExpiryDate,
                row.Contract.DepositAmount,
                row.Contract.DepositReturnDate,
                row.Contract.PeCode,
                row.Contract.PassCode,
                row.Contract.Status,
                row.Contract.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantContractDto?> GetTenantContractAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.TenantContracts.AsNoTracking()
            .Join(dbContext.Properties.AsNoTracking(),
                contract => contract.PropertyId,
                property => property.Id,
                (contract, property) => new { Contract = contract, Property = property })
            .Where(row => row.Contract.Id == id)
            .Select(row => new TenantContractDto(
                row.Contract.Id,
                row.Contract.PropertyId,
                row.Property.Code,
                row.Property.Project,
                row.Property.Area,
                row.Contract.TenantName,
                row.Contract.ManagerName,
                row.Contract.RentalPrice,
                row.Contract.SignedDate,
                row.Contract.TermMonths,
                row.Contract.ExpiryDate,
                row.Contract.DepositAmount,
                row.Contract.DepositReturnDate,
                row.Contract.PeCode,
                row.Contract.PassCode,
                row.Contract.Status,
                row.Contract.Notes))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<IReadOnlyList<TenantContractDto>> ListTenantContractsForPropertyAsync(Guid propertyId, CancellationToken cancellationToken)
        => ListTenantContractsAsync(new ContractFilterQuery(PropertyId: propertyId), cancellationToken);

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
}
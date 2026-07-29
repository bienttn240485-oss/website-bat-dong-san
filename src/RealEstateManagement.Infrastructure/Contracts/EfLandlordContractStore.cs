using RealEstateManagement.Application.Contracts;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace RealEstateManagement.Infrastructure.Contracts;

public sealed class EfLandlordContractStore(ApplicationDbContext dbContext) : ILandlordContractStore
{
    public async Task<IReadOnlyList<LandlordContractDto>> ListLandlordContractsAsync(ContractFilterQuery query, CancellationToken cancellationToken)
    {
        var rows = dbContext.LandlordContracts.AsNoTracking()
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
                row.Contract.LandlordName.Contains(keyword)
                || row.Property.Code.Contains(keyword)
                || row.Property.Area.Contains(keyword)
                || (row.Contract.PeCode != null && row.Contract.PeCode.Contains(keyword))
                || (row.Contract.SaleName != null && row.Contract.SaleName.Contains(keyword)));
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

        if (query.DepositStatus is not null)
        {
            rows = rows.Where(row => row.Contract.DepositStatus == query.DepositStatus);
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
            .Select(row => new LandlordContractDto(
                row.Contract.Id,
                row.Contract.PropertyId,
                row.Property.Code,
                row.Property.Project,
                row.Property.Area,
                row.Contract.LandlordName,
                row.Contract.PeCode,
                row.Contract.SaleName,
                row.Contract.InputPrice,
                row.Contract.SignedDate,
                row.Contract.ExpiryDate,
                row.Contract.DepositStatus,
                row.Contract.PaymentDay,
                row.Contract.PaymentWindow,
                row.Contract.NextDueDate,
                row.Contract.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<LandlordContractDto?> GetLandlordContractAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.LandlordContracts.AsNoTracking()
            .Join(dbContext.Properties.AsNoTracking(),
                contract => contract.PropertyId,
                property => property.Id,
                (contract, property) => new { Contract = contract, Property = property })
            .Where(row => row.Contract.Id == id)
            .Select(row => new LandlordContractDto(
                row.Contract.Id,
                row.Contract.PropertyId,
                row.Property.Code,
                row.Property.Project,
                row.Property.Area,
                row.Contract.LandlordName,
                row.Contract.PeCode,
                row.Contract.SaleName,
                row.Contract.InputPrice,
                row.Contract.SignedDate,
                row.Contract.ExpiryDate,
                row.Contract.DepositStatus,
                row.Contract.PaymentDay,
                row.Contract.PaymentWindow,
                row.Contract.NextDueDate,
                row.Contract.Notes))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<LandlordContractDto?> GetLandlordContractForPropertyAsync(Guid propertyId, CancellationToken cancellationToken)
        => await dbContext.LandlordContracts.AsNoTracking()
            .Join(dbContext.Properties.AsNoTracking(),
                contract => contract.PropertyId,
                property => property.Id,
                (contract, property) => new { Contract = contract, Property = property })
            .Where(row => row.Contract.PropertyId == propertyId)
            .Select(row => new LandlordContractDto(
                row.Contract.Id,
                row.Contract.PropertyId,
                row.Property.Code,
                row.Property.Project,
                row.Property.Area,
                row.Contract.LandlordName,
                row.Contract.PeCode,
                row.Contract.SaleName,
                row.Contract.InputPrice,
                row.Contract.SignedDate,
                row.Contract.ExpiryDate,
                row.Contract.DepositStatus,
                row.Contract.PaymentDay,
                row.Contract.PaymentWindow,
                row.Contract.NextDueDate,
                row.Contract.Notes))
            .FirstOrDefaultAsync(cancellationToken);

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
}
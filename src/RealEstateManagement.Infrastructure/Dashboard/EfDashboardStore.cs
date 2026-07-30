using Microsoft.EntityFrameworkCore;
using RealEstateManagement.Application.Dashboard;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Infrastructure.Data;

namespace RealEstateManagement.Infrastructure.Dashboard;

public sealed class EfDashboardStore(ApplicationDbContext dbContext) : IDashboardStore
{
    public async Task<DashboardSourceDto> GetDashboardSourceAsync(DateOnly today, int expiringWithinDays, CancellationToken cancellationToken)
        => await GetDashboardSourceAsync(today, expiringWithinDays, new DashboardScope(), cancellationToken);

    public async Task<DashboardSourceDto> GetDashboardSourceAsync(DateOnly today, int expiringWithinDays, DashboardScope scope, CancellationToken cancellationToken)
    {
        var properties = await dbContext.Properties
            .AsNoTracking()
            .Select(property => new DashboardPropertySourceDto(
                property.Id,
                property.Code,
                property.Project,
                property.Area,
                property.Type,
                property.Status,
                property.MonthlyPrice,
                property.SalePrice,
                property.AvailableFromDate))
            .ToListAsync(cancellationToken);

        var landlordContracts = await dbContext.LandlordContracts
            .AsNoTracking()
            .Join(
                dbContext.Properties.AsNoTracking(),
                contract => contract.PropertyId,
                property => property.Id,
                (contract, property) => new DashboardLandlordContractSourceDto(
                contract.Id,
                contract.PropertyId,
                property.Code,
                contract.InputPrice,
                contract.SignedDate,
                contract.ExpiryDate))
            .ToListAsync(cancellationToken);

        var tenantContracts = await dbContext.TenantContracts
            .AsNoTracking()
            .Join(
                dbContext.Properties.AsNoTracking(),
                contract => contract.PropertyId,
                property => property.Id,
                (contract, property) => new DashboardTenantContractSourceDto(
                contract.Id,
                contract.PropertyId,
                property.Code,
                contract.RentalPrice,
                contract.SignedDate,
                contract.TermMonths,
                contract.ExpiryDate,
                contract.DepositReturnDate,
                contract.Status))
            .ToListAsync(cancellationToken);

        var leadQuery = dbContext.Leads
            .AsNoTracking()
            .Where(lead => scope.AssignedToUserId == null || lead.AssignedToUserId == scope.AssignedToUserId);

        var leads = await leadQuery
            .Select(lead => new DashboardLeadSourceDto(
                lead.Id,
                lead.PropertyId,
                lead.Status,
                lead.AssignedToUserId,
                lead.CreatedAtUtc,
                lead.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new DashboardSourceDto(properties, landlordContracts, tenantContracts, leads);
    }
}

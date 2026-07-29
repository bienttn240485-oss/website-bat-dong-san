using RealEstateManagement.Application.Dashboard;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Domain.Properties;
using RealEstateManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace RealEstateManagement.Infrastructure.Dashboard;

public sealed class EfDashboardStore(ApplicationDbContext dbContext) : IDashboardStore
{
    public async Task<DashboardSourceDto> GetDashboardSourceAsync(DateOnly today, int expiringWithinDays, CancellationToken cancellationToken)
    {
        var totalProperties = await dbContext.Properties.CountAsync(cancellationToken);
        var availableProperties = await dbContext.Properties.CountAsync(property => property.Status == PropertyStatus.Available, cancellationToken);
        var occupiedProperties = await dbContext.Properties.CountAsync(property => property.Status == PropertyStatus.Occupied, cancellationToken);
        var soonAvailableProperties = await dbContext.Properties.CountAsync(property => property.Status == PropertyStatus.SoonAvailable, cancellationToken);
        var activeTenantContracts = await dbContext.TenantContracts.CountAsync(contract => contract.Status == ContractStatus.Active, cancellationToken);
        var expiringUntil = today.AddDays(expiringWithinDays);
        var expiringTenantContracts = await dbContext.TenantContracts.CountAsync(
            contract => contract.Status == ContractStatus.Active && contract.ExpiryDate <= expiringUntil,
            cancellationToken);

        var leadGroups = await dbContext.Leads
            .GroupBy(lead => lead.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var monthlyInputTotal = await dbContext.LandlordContracts.SumAsync(contract => (long?)contract.InputPrice, cancellationToken) ?? 0;
        var monthlyRentTotal = await dbContext.TenantContracts
            .Where(contract => contract.Status == ContractStatus.Active)
            .SumAsync(contract => (long?)contract.RentalPrice, cancellationToken) ?? 0;

        return new DashboardSourceDto(
            totalProperties,
            availableProperties,
            occupiedProperties,
            soonAvailableProperties,
            activeTenantContracts,
            expiringTenantContracts,
            leadGroups.ToDictionary(group => group.Status, group => group.Count),
            monthlyInputTotal,
            monthlyRentTotal);
    }
}

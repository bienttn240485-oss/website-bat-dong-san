using RealEstateManagement.Domain.Leads;

namespace RealEstateManagement.Application.Dashboard;

public sealed record DashboardSnapshotDto(
    int TotalProperties,
    int AvailableProperties,
    int OccupiedProperties,
    int SoonAvailableProperties,
    int ActiveTenantContracts,
    int ExpiringTenantContracts,
    IReadOnlyDictionary<LeadStatus, int> LeadsByStatus,
    long MonthlyInputTotal,
    long MonthlyRentTotal,
    long MonthlyPriceSpread);

public sealed record DashboardSourceDto(
    int TotalProperties,
    int AvailableProperties,
    int OccupiedProperties,
    int SoonAvailableProperties,
    int ActiveTenantContracts,
    int ExpiringTenantContracts,
    IReadOnlyDictionary<LeadStatus, int> LeadsByStatus,
    long MonthlyInputTotal,
    long MonthlyRentTotal);

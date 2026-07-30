using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Dashboard;

public sealed record DashboardSnapshotDto(
    DashboardOverviewDto Overview,
    DashboardFinancialSummaryDto Financial,
    IReadOnlyList<DashboardWarningDto> Warnings,
    IReadOnlyList<DashboardTimelineItemDto> Timeline,
    DashboardChartsDto Charts);

public sealed record DashboardScope(Guid? AssignedToUserId = null);

public sealed record DashboardOverviewDto(
    int TotalProperties,
    int AvailableProperties,
    int OccupiedProperties,
    int SoonAvailableProperties,
    int ReservedProperties,
    int TotalLandlordContracts,
    int ActiveTenantContracts,
    int TotalLeads,
    int NewLeads,
    int UnassignedLeads,
    int PropertiesForSale,
    int PropertiesForRent);

public sealed record DashboardFinancialSummaryDto(
    long MonthlyInputTotal,
    long MonthlyRentTotal,
    long MonthlySpread,
    int NegativeMarginProperties,
    long AverageMonthlySpread,
    long TenantContractGmvLast12Months);

public sealed record DashboardWarningDto(
    string Type,
    string Title,
    string Description,
    string Tone,
    string? Link);

public sealed record DashboardTimelineItemDto(
    string EventType,
    string PropertyCode,
    DateOnly Date,
    string Description,
    string Link,
    string Tone);

public sealed record DashboardChartsDto(
    DashboardChartDto GmvLast12Months,
    DashboardChartDto ContractsSignedLast12Months,
    DashboardChartDto PropertyStatusDistribution,
    DashboardChartDto LeadStatusDistribution);

public sealed record DashboardChartDto(
    IReadOnlyList<string> Labels,
    IReadOnlyList<DashboardChartDatasetDto> Datasets);

public sealed record DashboardChartDatasetDto(string Label, string Type, IReadOnlyList<long> Data, string Tone);

public sealed record DashboardSourceDto(
    IReadOnlyList<DashboardPropertySourceDto> Properties,
    IReadOnlyList<DashboardLandlordContractSourceDto> LandlordContracts,
    IReadOnlyList<DashboardTenantContractSourceDto> TenantContracts,
    IReadOnlyList<DashboardLeadSourceDto> Leads);

public sealed record DashboardPropertySourceDto(
    Guid Id,
    string Code,
    PropertyProject? Project,
    string Area,
    PropertyType Type,
    PropertyStatus Status,
    long? MonthlyPrice,
    long? SalePrice,
    DateOnly? AvailableFromDate);

public sealed record DashboardLandlordContractSourceDto(
    Guid Id,
    Guid PropertyId,
    string PropertyCode,
    long InputPrice,
    DateOnly SignedDate,
    DateOnly ExpiryDate);

public sealed record DashboardTenantContractSourceDto(
    Guid Id,
    Guid PropertyId,
    string PropertyCode,
    long RentalPrice,
    DateOnly SignedDate,
    int TermMonths,
    DateOnly ExpiryDate,
    DateOnly? DepositReturnDate,
    ContractStatus Status);

public sealed record DashboardLeadSourceDto(
    Guid Id,
    Guid? PropertyId,
    LeadStatus Status,
    Guid? AssignedToUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

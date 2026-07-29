using RealEstateManagement.Application.Common.Time;

namespace RealEstateManagement.Application.Dashboard;

public sealed class DashboardService(IDashboardStore store, ISystemClock clock) : IDashboardService
{
    private const int ExpiringWithinDays = 30;

    public async Task<DashboardSnapshotDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, BusinessTimeZone()).DateTime);
        var source = await store.GetDashboardSourceAsync(today, ExpiringWithinDays, cancellationToken);

        return new DashboardSnapshotDto(
            source.TotalProperties,
            source.AvailableProperties,
            source.OccupiedProperties,
            source.SoonAvailableProperties,
            source.ActiveTenantContracts,
            source.ExpiringTenantContracts,
            source.LeadsByStatus,
            source.MonthlyInputTotal,
            source.MonthlyRentTotal,
            source.MonthlyRentTotal - source.MonthlyInputTotal);
    }

    private static TimeZoneInfo BusinessTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}

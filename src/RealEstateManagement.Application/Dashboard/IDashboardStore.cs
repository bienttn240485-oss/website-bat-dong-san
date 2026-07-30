namespace RealEstateManagement.Application.Dashboard;

public interface IDashboardStore
{
    Task<DashboardSourceDto> GetDashboardSourceAsync(DateOnly today, int expiringWithinDays, CancellationToken cancellationToken);

    Task<DashboardSourceDto> GetDashboardSourceAsync(DateOnly today, int expiringWithinDays, DashboardScope scope, CancellationToken cancellationToken);
}

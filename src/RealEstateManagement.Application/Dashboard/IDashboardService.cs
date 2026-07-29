namespace RealEstateManagement.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSnapshotDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

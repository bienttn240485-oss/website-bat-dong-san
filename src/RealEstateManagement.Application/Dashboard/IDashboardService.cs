namespace RealEstateManagement.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSnapshotDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<DashboardSnapshotDto> GetDashboardAsync(DashboardScope scope, CancellationToken cancellationToken = default);
}

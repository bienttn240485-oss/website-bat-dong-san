using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Dashboard;
using RealEstateManagement.Domain.Leads;

namespace RealEstateManagement.Tests.Application;

public sealed class DashboardServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetDashboardAsync_CalculatesBasicMetrics()
    {
        var store = new InMemoryDashboardStore
        {
            Source = new DashboardSourceDto(
                10,
                4,
                5,
                1,
                5,
                2,
                new Dictionary<LeadStatus, int>
                {
                    [LeadStatus.New] = 3,
                    [LeadStatus.Contacted] = 2
                },
                80_000_000,
                110_000_000)
        };
        var service = new DashboardService(store, new FixedClock());

        var dashboard = await service.GetDashboardAsync();

        Assert.Equal(10, dashboard.TotalProperties);
        Assert.Equal(4, dashboard.AvailableProperties);
        Assert.Equal(5, dashboard.OccupiedProperties);
        Assert.Equal(1, dashboard.SoonAvailableProperties);
        Assert.Equal(5, dashboard.ActiveTenantContracts);
        Assert.Equal(2, dashboard.ExpiringTenantContracts);
        Assert.Equal(3, dashboard.LeadsByStatus[LeadStatus.New]);
        Assert.Equal(30_000_000, dashboard.MonthlyPriceSpread);
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class InMemoryDashboardStore : IDashboardStore
    {
        public DashboardSourceDto Source { get; init; } = new(0, 0, 0, 0, 0, 0, new Dictionary<LeadStatus, int>(), 0, 0);

        public Task<DashboardSourceDto> GetDashboardSourceAsync(DateOnly today, int expiringWithinDays, CancellationToken cancellationToken)
        {
            Assert.Equal(new DateOnly(2026, 7, 28), today);
            Assert.Equal(30, expiringWithinDays);
            return Task.FromResult(Source);
        }
    }
}

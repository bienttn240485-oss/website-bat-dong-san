using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Dashboard;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Tests.Application;

public sealed class DashboardServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid PropertyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PropertyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PropertyC = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task GetDashboardAsync_CountsOverviewMetrics()
    {
        var dashboard = await CreateService(Source(
            properties:
            [
                Property(PropertyA, "OP-0101", PropertyStatus.Available, monthlyPrice: 18_000_000),
                Property(PropertyB, "ORI-1808", PropertyStatus.Occupied, salePrice: 5_600_000_000),
                Property(PropertyC, "GH-2203", PropertyStatus.SoonAvailable, monthlyPrice: 22_000_000, salePrice: 6_200_000_000),
                Property(Guid.NewGuid(), "VH-0901", PropertyStatus.Reserved)
            ],
            landlords: [Landlord(PropertyA, inputPrice: 12_000_000)],
            tenants: [Tenant(PropertyB, rentalPrice: 20_000_000, status: ContractStatus.Active)],
            leads:
            [
                Lead(LeadStatus.New, assignedToUserId: null),
                Lead(LeadStatus.Contacted, assignedToUserId: Guid.NewGuid())
            ])).GetDashboardAsync();

        Assert.Equal(4, dashboard.Overview.TotalProperties);
        Assert.Equal(1, dashboard.Overview.AvailableProperties);
        Assert.Equal(1, dashboard.Overview.OccupiedProperties);
        Assert.Equal(1, dashboard.Overview.SoonAvailableProperties);
        Assert.Equal(1, dashboard.Overview.ReservedProperties);
        Assert.Equal(1, dashboard.Overview.TotalLandlordContracts);
        Assert.Equal(1, dashboard.Overview.ActiveTenantContracts);
        Assert.Equal(2, dashboard.Overview.TotalLeads);
        Assert.Equal(1, dashboard.Overview.NewLeads);
        Assert.Equal(1, dashboard.Overview.UnassignedLeads);
        Assert.Equal(2, dashboard.Overview.PropertiesForSale);
        Assert.Equal(2, dashboard.Overview.PropertiesForRent);
    }

    [Fact]
    public async Task GetDashboardAsync_CalculatesFinancialSummary()
    {
        var dashboard = await CreateService(Source(
            properties: [Property(PropertyA, "OP-0101", PropertyStatus.Occupied), Property(PropertyB, "ORI-1808", PropertyStatus.Occupied)],
            landlords: [Landlord(PropertyA, inputPrice: 12_000_000), Landlord(PropertyB, inputPrice: 25_000_000)],
            tenants:
            [
                Tenant(PropertyA, rentalPrice: 18_000_000, termMonths: 12, signedDate: new DateOnly(2026, 7, 1), status: ContractStatus.Active),
                Tenant(PropertyB, rentalPrice: 20_000_000, termMonths: 6, signedDate: new DateOnly(2026, 7, 1), status: ContractStatus.Active)
            ])).GetDashboardAsync();

        Assert.Equal(37_000_000, dashboard.Financial.MonthlyInputTotal);
        Assert.Equal(38_000_000, dashboard.Financial.MonthlyRentTotal);
        Assert.Equal(1_000_000, dashboard.Financial.MonthlySpread);
        Assert.Equal(1, dashboard.Financial.NegativeMarginProperties);
        Assert.Equal(500_000, dashboard.Financial.AverageMonthlySpread);
    }

    [Fact]
    public async Task GetDashboardAsync_ExcludesCancelledContractsFromGmv()
    {
        var dashboard = await CreateService(Source(
            tenants:
            [
                Tenant(PropertyA, rentalPrice: 10_000_000, termMonths: 12, signedDate: new DateOnly(2026, 7, 2), status: ContractStatus.Active),
                Tenant(PropertyB, rentalPrice: 99_000_000, termMonths: 12, signedDate: new DateOnly(2026, 7, 2), status: ContractStatus.Cancelled)
            ])).GetDashboardAsync();

        Assert.Equal(120_000_000, dashboard.Financial.TenantContractGmvLast12Months);
    }

    [Fact]
    public async Task GetDashboardAsync_WarnsForContractExpiringWithin30Days()
    {
        var dashboard = await CreateService(Source(
            landlords: [Landlord(PropertyA, expiryDate: new DateOnly(2026, 8, 20))],
            tenants: [Tenant(PropertyA, expiryDate: new DateOnly(2026, 8, 10), status: ContractStatus.Active)])).GetDashboardAsync();

        Assert.Contains(dashboard.Warnings, warning => warning.Type == "LandlordContractExpiring");
        Assert.Contains(dashboard.Warnings, warning => warning.Type == "TenantContractExpiring");
    }

    [Fact]
    public async Task GetDashboardAsync_DoesNotWarnForContractOutside30Days()
    {
        var dashboard = await CreateService(Source(
            landlords: [Landlord(PropertyA, expiryDate: new DateOnly(2026, 9, 1))],
            tenants: [Tenant(PropertyA, expiryDate: new DateOnly(2026, 9, 5), status: ContractStatus.Active)])).GetDashboardAsync();

        Assert.DoesNotContain(dashboard.Warnings, warning => warning.Type == "LandlordContractExpiring");
        Assert.DoesNotContain(dashboard.Warnings, warning => warning.Type == "TenantContractExpiring");
    }

    [Fact]
    public async Task GetDashboardAsync_WarnsForPropertyStatusMismatch()
    {
        var dashboard = await CreateService(Source(
            properties:
            [
                Property(PropertyA, "OP-0101", PropertyStatus.Occupied),
                Property(PropertyB, "ORI-1808", PropertyStatus.Available)
            ],
            tenants: [Tenant(PropertyB, status: ContractStatus.Active)])).GetDashboardAsync();

        Assert.Contains(dashboard.Warnings, warning => warning.Type == "OccupiedWithoutActiveTenant");
        Assert.Contains(dashboard.Warnings, warning => warning.Type == "AvailableWithActiveTenant");
    }

    [Fact]
    public async Task GetDashboardAsync_WarnsForOverdueNewLeadAndUnassignedLead()
    {
        var dashboard = await CreateService(Source(
            leads: [Lead(LeadStatus.New, assignedToUserId: null, createdAtUtc: Now.AddDays(-4))])).GetDashboardAsync();

        Assert.Contains(dashboard.Warnings, warning => warning.Type == "OverdueNewLead");
        Assert.Contains(dashboard.Warnings, warning => warning.Type == "UnassignedLead");
    }

    [Fact]
    public async Task GetDashboardAsync_SortsTimelineByDateThenPropertyCode()
    {
        var dashboard = await CreateService(Source(
            properties: [Property(PropertyB, "ORI-1808", PropertyStatus.SoonAvailable, availableFromDate: new DateOnly(2026, 9, 1))],
            landlords: [Landlord(PropertyA, propertyCode: "OP-0101", expiryDate: new DateOnly(2026, 8, 15))],
            tenants: [Tenant(PropertyC, propertyCode: "GH-2203", expiryDate: new DateOnly(2026, 8, 15), status: ContractStatus.Active)])).GetDashboardAsync();

        Assert.Collection(
            dashboard.Timeline.Take(3),
            item => Assert.Equal("GH-2203", item.PropertyCode),
            item => Assert.Equal("OP-0101", item.PropertyCode),
            item => Assert.Equal("ORI-1808", item.PropertyCode));
    }

    private static DashboardService CreateService(DashboardSourceDto source)
        => new(new InMemoryDashboardStore(source), new FixedClock());

    private static DashboardSourceDto Source(
        IReadOnlyList<DashboardPropertySourceDto>? properties = null,
        IReadOnlyList<DashboardLandlordContractSourceDto>? landlords = null,
        IReadOnlyList<DashboardTenantContractSourceDto>? tenants = null,
        IReadOnlyList<DashboardLeadSourceDto>? leads = null)
        => new(properties ?? [], landlords ?? [], tenants ?? [], leads ?? []);

    private static DashboardPropertySourceDto Property(
        Guid propertyId,
        string code,
        PropertyStatus status,
        long? monthlyPrice = null,
        long? salePrice = null,
        DateOnly? availableFromDate = null)
        => new(propertyId, code, PropertyProject.Origami, "S1", PropertyType.TwoBedroomTwoBathrooms, status, monthlyPrice, salePrice, availableFromDate);

    private static DashboardLandlordContractSourceDto Landlord(
        Guid propertyId,
        string propertyCode = "OP-0101",
        long inputPrice = 12_000_000,
        DateOnly? signedDate = null,
        DateOnly? expiryDate = null)
        => new(Guid.NewGuid(), propertyId, propertyCode, inputPrice, signedDate ?? new DateOnly(2026, 1, 1), expiryDate ?? new DateOnly(2027, 1, 1));

    private static DashboardTenantContractSourceDto Tenant(
        Guid propertyId,
        string propertyCode = "OP-0101",
        long rentalPrice = 18_000_000,
        int termMonths = 12,
        DateOnly? signedDate = null,
        DateOnly? expiryDate = null,
        DateOnly? depositReturnDate = null,
        ContractStatus status = ContractStatus.Active)
        => new(Guid.NewGuid(), propertyId, propertyCode, rentalPrice, signedDate ?? new DateOnly(2026, 1, 1), termMonths, expiryDate ?? new DateOnly(2027, 1, 1), depositReturnDate, status);

    private static DashboardLeadSourceDto Lead(LeadStatus status, Guid? assignedToUserId, DateTimeOffset? createdAtUtc = null)
        => new(Guid.NewGuid(), null, status, assignedToUserId, createdAtUtc ?? Now, createdAtUtc ?? Now);

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class InMemoryDashboardStore(DashboardSourceDto source) : IDashboardStore
    {
        public Task<DashboardSourceDto> GetDashboardSourceAsync(DateOnly today, int expiringWithinDays, CancellationToken cancellationToken)
        {
            Assert.Equal(new DateOnly(2026, 7, 28), today);
            Assert.Equal(30, expiringWithinDays);
            return Task.FromResult(source);
        }
    }
}

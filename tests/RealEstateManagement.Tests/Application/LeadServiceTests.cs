using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Leads;
using RealEstateManagement.Domain.Leads;

namespace RealEstateManagement.Tests.Application;

public sealed class LeadServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateLeadAsync_WhenValid_AddsLeadWithNewStatus()
    {
        var store = new InMemoryLeadStore();
        var propertyId = Guid.NewGuid();
        store.PropertyIds.Add(propertyId);
        var service = new LeadService(store, new FixedClock());

        var result = await service.CreateLeadAsync(new LeadCreateCommand("Le Van C", "0909000000", propertyId, "Rent", "Need viewing", null));

        Assert.True(result.Succeeded);
        var lead = Assert.Single(store.Leads);
        Assert.Equal(LeadStatus.New, lead.Status);
        Assert.Equal("vi", lead.Language);
    }

    [Fact]
    public async Task AssignLeadAsync_WhenSaleUserExists_AssignsLead()
    {
        var store = new InMemoryLeadStore();
        var saleUserId = Guid.NewGuid();
        var lead = CreateLead();
        store.SaleUserIds.Add(saleUserId);
        store.Leads.Add(lead);
        var service = new LeadService(store, new FixedClock());

        var result = await service.AssignLeadAsync(new LeadAssignmentCommand(lead.Id, saleUserId));

        Assert.True(result.Succeeded);
        Assert.Equal(saleUserId, lead.AssignedToUserId);
    }

    [Fact]
    public async Task UpdateLeadAsync_WhenValid_UpdatesLeadDetails()
    {
        var store = new InMemoryLeadStore();
        var lead = CreateLead();
        store.Leads.Add(lead);
        var service = new LeadService(store, new FixedClock());

        var result = await service.UpdateLeadAsync(lead.Id, new LeadUpdateCommand("Nguyen Van D", "0911000000", null, "Buy", "Need sale options", "vi"));

        Assert.True(result.Succeeded);
        Assert.Equal("Nguyen Van D", lead.Name);
        Assert.Equal("0911000000", lead.Contact);
        Assert.Equal("Buy", lead.Subject);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenValid_ChangesLeadStatus()
    {
        var store = new InMemoryLeadStore();
        var lead = CreateLead();
        store.Leads.Add(lead);
        var service = new LeadService(store, new FixedClock());

        var result = await service.ChangeStatusAsync(new LeadStatusCommand(lead.Id, LeadStatus.Contacted));

        Assert.True(result.Succeeded);
        Assert.Equal(LeadStatus.Contacted, lead.Status);
    }

    private static Lead CreateLead()
        => new(Guid.NewGuid(), "Le Van C", "0909000000", null, null, null, null, Now);

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class InMemoryLeadStore : ILeadStore
    {
        public List<Lead> Leads { get; } = [];
        public HashSet<Guid> PropertyIds { get; } = [];
        public HashSet<Guid> SaleUserIds { get; } = [];

        public Task<IReadOnlyList<LeadDto>> ListLeadsAsync(LeadFilterQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<LeadDto>>([]);

        public Task<LeadDto?> GetLeadAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<LeadDto?>(null);

        public Task<Lead?> GetLeadForUpdateAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Leads.FirstOrDefault(lead => lead.Id == id));

        public Task<bool> PropertyExistsAsync(Guid propertyId, CancellationToken cancellationToken)
            => Task.FromResult(PropertyIds.Contains(propertyId));

        public Task<bool> SaleUserExistsAsync(Guid saleUserId, CancellationToken cancellationToken)
            => Task.FromResult(SaleUserIds.Contains(saleUserId));

        public Task AddLeadAsync(Lead lead, CancellationToken cancellationToken)
        {
            Leads.Add(lead);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

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

        var result = await service.CreateLeadAsync(new LeadCreateCommand("Lê Văn C", "0909000000", propertyId, "Thuê căn", "Cần xem căn", null));

        Assert.True(result.Succeeded);
        var lead = Assert.Single(store.Leads);
        Assert.Equal(LeadStatus.New, lead.Status);
        Assert.Equal("vi", lead.Language);
    }

    [Theory]
    [InlineData("", "0909000000", "Vui lòng nhập tên khách hàng.")]
    [InlineData("Lê Văn C", "", "Vui lòng nhập thông tin liên hệ.")]
    public async Task CreateLeadAsync_WhenRequiredFieldEmpty_ReturnsValidation(string name, string contact, string expectedError)
    {
        var service = new LeadService(new InMemoryLeadStore(), new FixedClock());

        var result = await service.CreateLeadAsync(new LeadCreateCommand(name, contact, null, null, null, null));

        Assert.False(result.Succeeded);
        Assert.Contains(expectedError, result.Errors);
    }

    [Fact]
    public async Task CreateLeadAsync_WhenPropertyMissing_ReturnsValidation()
    {
        var service = new LeadService(new InMemoryLeadStore(), new FixedClock());

        var result = await service.CreateLeadAsync(new LeadCreateCommand("Lê Văn C", "0909000000", Guid.NewGuid(), null, null, "vi"));

        Assert.False(result.Succeeded);
        Assert.Contains("Không tìm thấy căn hộ liên quan đến lead.", result.Errors);
    }

    [Fact]
    public async Task CreateLeadAsync_WhenNoProperty_CreatesLead()
    {
        var store = new InMemoryLeadStore();
        var service = new LeadService(store, new FixedClock());

        var result = await service.CreateLeadAsync(new LeadCreateCommand("Lê Văn C", "0909000000", null, null, null, null));

        Assert.True(result.Succeeded);
        Assert.Null(Assert.Single(store.Leads).PropertyId);
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
    public async Task AssignLeadAsync_WhenSaleUserMissing_ReturnsValidation()
    {
        var store = new InMemoryLeadStore();
        var lead = CreateLead();
        store.Leads.Add(lead);
        var service = new LeadService(store, new FixedClock());

        var result = await service.AssignLeadAsync(new LeadAssignmentCommand(lead.Id, Guid.NewGuid()));

        Assert.False(result.Succeeded);
        Assert.Contains("Không tìm thấy nhân viên Sale được phân công.", result.Errors);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenValid_ChangesLeadStatusAndTimestamp()
    {
        var store = new InMemoryLeadStore();
        var lead = CreateLead();
        store.Leads.Add(lead);
        var service = new LeadService(store, new FixedClock());

        var result = await service.ChangeStatusAsync(new LeadStatusCommand(lead.Id, LeadStatus.Contacted));

        Assert.True(result.Succeeded);
        Assert.Equal(LeadStatus.Contacted, lead.Status);
        Assert.Equal(Now, lead.UpdatedAtUtc);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenStaffDoesNotOwnLead_ReturnsAuthorizationError()
    {
        var store = new InMemoryLeadStore();
        var lead = CreateLead();
        lead.AssignTo(Guid.NewGuid(), Now);
        store.Leads.Add(lead);
        var service = new LeadService(store, new FixedClock());

        var result = await service.ChangeStatusAsync(new LeadStatusCommand(lead.Id, LeadStatus.Contacted, Guid.NewGuid(), ActorCanManageAll: false));

        Assert.False(result.Succeeded);
        Assert.Contains("Bạn không có quyền cập nhật lead này.", result.Errors);
    }

    [Fact]
    public async Task ListLeadsAsync_FiltersByStatusAssignedPropertyAndKeyword()
    {
        var assignedUserId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var matching = CreateLead(propertyId: propertyId, name: "Nguyễn Minh An", subject: "Tìm căn thuê", createdAt: Now.AddMinutes(2));
        matching.AssignTo(assignedUserId, Now);
        matching.ChangeStatus(LeadStatus.Contacted, Now);
        var store = new InMemoryLeadStore();
        store.Leads.Add(matching);
        store.Leads.Add(CreateLead(name: "Khách khác", subject: "Mua căn"));
        var service = new LeadService(store, new FixedClock());

        var result = await service.ListLeadsAsync(new LeadFilterQuery(LeadStatus.Contacted, propertyId, assignedUserId, Keyword: "Minh"));

        var lead = Assert.Single(result);
        Assert.Equal(matching.Id, lead.Id);
    }

    [Fact]
    public async Task ListLeadsAsync_WhenNewestFirst_SortsByCreatedAtDescending()
    {
        var older = CreateLead(name: "Cũ", createdAt: Now.AddHours(-1));
        var newer = CreateLead(name: "Mới", createdAt: Now.AddHours(1));
        var store = new InMemoryLeadStore();
        store.Leads.Add(older);
        store.Leads.Add(newer);
        var service = new LeadService(store, new FixedClock());

        var result = await service.ListLeadsAsync(new LeadFilterQuery(NewestFirst: true));

        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    private static Lead CreateLead(Guid? propertyId = null, string name = "Lê Văn C", string contact = "0909000000", string? subject = "Thuê căn", DateTimeOffset? createdAt = null)
        => new(Guid.NewGuid(), name, contact, propertyId, subject, "Cần tư vấn", null, createdAt ?? Now);

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
        {
            IEnumerable<Lead> leads = Leads;
            if (query.Status is not null)
            {
                leads = leads.Where(lead => lead.Status == query.Status);
            }

            if (query.PropertyId is not null)
            {
                leads = leads.Where(lead => lead.PropertyId == query.PropertyId);
            }

            if (query.AssignedToUserId is not null)
            {
                leads = leads.Where(lead => lead.AssignedToUserId == query.AssignedToUserId);
            }

            if (query.UnassignedOnly)
            {
                leads = leads.Where(lead => lead.AssignedToUserId is null);
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                leads = leads.Where(lead => lead.Name.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase)
                    || lead.Contact.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase)
                    || lead.Subject?.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase) == true);
            }

            leads = query.NewestFirst
                ? leads.OrderByDescending(lead => lead.CreatedAtUtc)
                : leads.OrderBy(lead => lead.CreatedAtUtc);

            return Task.FromResult<IReadOnlyList<LeadDto>>(leads.Select(ToDto).ToArray());
        }

        public Task<LeadDto?> GetLeadAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Leads.Where(lead => lead.Id == id).Select(ToDto).FirstOrDefault());

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

        private static LeadDto ToDto(Lead lead)
            => new(
                lead.Id,
                lead.Name,
                lead.Contact,
                lead.PropertyId,
                lead.Subject,
                lead.Message,
                lead.Language,
                lead.Status,
                lead.AssignedToUserId,
                lead.CreatedAtUtc,
                lead.UpdatedAtUtc);
    }
}

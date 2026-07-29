using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Application.Leads;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace RealEstateManagement.Infrastructure.Leads;

public sealed class EfLeadStore(ApplicationDbContext dbContext) : ILeadStore
{
    public async Task<IReadOnlyList<LeadDto>> ListLeadsAsync(LeadFilterQuery query, CancellationToken cancellationToken)
    {
        var leads = await ApplyFilter(dbContext.Leads.AsNoTracking(), query)
            .OrderByDescending(lead => lead.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return leads.Select(ToDto).ToArray();
    }

    public async Task<LeadDto?> GetLeadAsync(Guid id, CancellationToken cancellationToken)
    {
        var lead = await dbContext.Leads.AsNoTracking().FirstOrDefaultAsync(lead => lead.Id == id, cancellationToken);
        return lead is null ? null : ToDto(lead);
    }

    public Task<Lead?> GetLeadForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Leads.FirstOrDefaultAsync(lead => lead.Id == id, cancellationToken);

    public Task<bool> PropertyExistsAsync(Guid propertyId, CancellationToken cancellationToken)
        => dbContext.Properties.AnyAsync(property => property.Id == propertyId, cancellationToken);

    public Task<bool> SaleUserExistsAsync(Guid saleUserId, CancellationToken cancellationToken)
        => dbContext.Users.AnyAsync(user => user.Id == saleUserId
            && dbContext.UserRoles.Any(userRole => userRole.UserId == saleUserId
                && dbContext.Roles.Any(role => role.Id == userRole.RoleId
                    && (role.Name == "Sale" || role.Name == ApplicationRoles.Staff))),
            cancellationToken);

    public async Task AddLeadAsync(Lead lead, CancellationToken cancellationToken)
        => await dbContext.Leads.AddAsync(lead, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Lead> ApplyFilter(IQueryable<Lead> queryable, LeadFilterQuery query)
    {
        if (query.Status is not null)
        {
            queryable = queryable.Where(lead => lead.Status == query.Status);
        }

        if (query.PropertyId is not null)
        {
            queryable = queryable.Where(lead => lead.PropertyId == query.PropertyId);
        }

        if (query.AssignedToUserId is not null)
        {
            queryable = queryable.Where(lead => lead.AssignedToUserId == query.AssignedToUserId);
        }

        if (query.CreatedFrom is not null)
        {
            var from = query.CreatedFrom.Value.ToDateTime(TimeOnly.MinValue);
            queryable = queryable.Where(lead => lead.CreatedAtUtc >= new DateTimeOffset(from, TimeSpan.Zero));
        }

        if (query.CreatedTo is not null)
        {
            var toExclusive = query.CreatedTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            queryable = queryable.Where(lead => lead.CreatedAtUtc < new DateTimeOffset(toExclusive, TimeSpan.Zero));
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            queryable = queryable.Where(lead => lead.Name.Contains(keyword)
                || lead.Contact.Contains(keyword)
                || (lead.Subject != null && lead.Subject.Contains(keyword)));
        }

        return queryable;
    }

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

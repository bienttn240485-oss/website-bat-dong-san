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
        var leadQuery = ApplyFilter(dbContext.Leads.AsNoTracking(), query);

        var rows = await (
            from lead in leadQuery
            join property in dbContext.Properties.AsNoTracking() on lead.PropertyId equals property.Id into propertyGroup
            from property in propertyGroup.DefaultIfEmpty()
            join assignedUser in dbContext.Users.AsNoTracking() on lead.AssignedToUserId equals assignedUser.Id into userGroup
            from assignedUser in userGroup.DefaultIfEmpty()
            select new
            {
                Lead = lead,
                PropertyCode = property == null ? null : property.Code,
                PropertyProject = property == null ? null : (Domain.Properties.PropertyProject?)property.Project,
                PropertyArea = property == null ? null : property.Area,
                PropertyType = property == null ? null : (Domain.Properties.PropertyType?)property.Type,
                PropertyMonthlyPrice = property == null ? null : property.MonthlyPrice,
                PropertySalePrice = property == null ? null : property.SalePrice,
                PropertyStatus = property == null ? null : (Domain.Properties.PropertyStatus?)property.Status,
                AssignedDisplayName = assignedUser == null ? null : (assignedUser.DisplayName ?? assignedUser.FullName),
                AssignedEmail = assignedUser == null ? null : assignedUser.Email
            }).ToListAsync(cancellationToken);

        var orderedRows = query.NewestFirst
            ? rows.OrderByDescending(row => row.Lead.CreatedAtUtc)
            : rows.OrderBy(row => row.Lead.CreatedAtUtc);

        return orderedRows.Select(row => ToDto(
                row.Lead,
                row.PropertyCode,
                row.PropertyProject,
                row.PropertyArea,
                row.PropertyType,
                row.PropertyMonthlyPrice,
                row.PropertySalePrice,
                row.PropertyStatus,
                row.AssignedDisplayName,
                row.AssignedEmail))
            .ToArray();
    }

    public async Task<LeadDto?> GetLeadAsync(Guid id, CancellationToken cancellationToken)
    {
        return (await ListLeadsAsync(new LeadFilterQuery(), cancellationToken))
            .FirstOrDefault(lead => lead.Id == id);
    }

    public Task<Lead?> GetLeadForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Leads.FirstOrDefaultAsync(lead => lead.Id == id, cancellationToken);

    public Task<bool> PropertyExistsAsync(Guid propertyId, CancellationToken cancellationToken)
        => dbContext.Properties.AnyAsync(property => property.Id == propertyId, cancellationToken);

    public Task<bool> SaleUserExistsAsync(Guid saleUserId, CancellationToken cancellationToken)
        => dbContext.Users.AnyAsync(user => user.Id == saleUserId
            && dbContext.UserRoles.Any(userRole => userRole.UserId == saleUserId
                && dbContext.Roles.Any(role => role.Id == userRole.RoleId
                    && (role.Name == "Sale"
                        || role.Name == "Admin"
                        || role.Name == ApplicationRoles.Staff
                        || role.Name == ApplicationRoles.Owner))),
            cancellationToken);

    public async Task AddLeadAsync(Lead lead, CancellationToken cancellationToken)
        => await dbContext.Leads.AddAsync(lead, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Lead> ApplyFilter(IQueryable<Lead> queryable, LeadFilterQuery query)
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

        if (query.UnassignedOnly)
        {
            queryable = queryable.Where(lead => lead.AssignedToUserId == null);
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

        if (query.Project is not null)
        {
            queryable = queryable.Where(lead => lead.PropertyId != null
                && dbContext.Properties.Any(property => property.Id == lead.PropertyId && property.Project == query.Project));
        }

        if (!string.IsNullOrWhiteSpace(query.Area))
        {
            var area = query.Area.Trim();
            queryable = queryable.Where(lead => lead.PropertyId != null
                && dbContext.Properties.Any(property => property.Id == lead.PropertyId && property.Area.Contains(area)));
        }

        if (!string.IsNullOrWhiteSpace(query.Language))
        {
            queryable = queryable.Where(lead => lead.Language == query.Language);
        }

        return queryable;
    }

    private static LeadDto ToDto(
        Lead lead,
        string? propertyCode = null,
        Domain.Properties.PropertyProject? propertyProject = null,
        string? propertyArea = null,
        Domain.Properties.PropertyType? propertyType = null,
        long? propertyMonthlyPrice = null,
        long? propertySalePrice = null,
        Domain.Properties.PropertyStatus? propertyStatus = null,
        string? assignedDisplayName = null,
        string? assignedEmail = null)
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
            lead.UpdatedAtUtc,
            propertyCode,
            propertyProject,
            propertyArea,
            propertyType,
            propertyMonthlyPrice,
            propertySalePrice,
            propertyStatus,
            assignedDisplayName,
            assignedEmail);
}

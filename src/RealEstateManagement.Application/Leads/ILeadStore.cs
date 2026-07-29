using RealEstateManagement.Domain.Leads;

namespace RealEstateManagement.Application.Leads;

public interface ILeadStore
{
    Task<IReadOnlyList<LeadDto>> ListLeadsAsync(LeadFilterQuery query, CancellationToken cancellationToken);
    Task<LeadDto?> GetLeadAsync(Guid id, CancellationToken cancellationToken);
    Task<Lead?> GetLeadForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> PropertyExistsAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<bool> SaleUserExistsAsync(Guid saleUserId, CancellationToken cancellationToken);
    Task AddLeadAsync(Lead lead, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

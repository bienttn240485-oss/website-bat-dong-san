using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Contracts;

public interface ITenantContractStore
{
    Task<IReadOnlyList<TenantContractDto>> ListTenantContractsAsync(ContractFilterQuery query, CancellationToken cancellationToken);
    Task<TenantContractDto?> GetTenantContractAsync(Guid id, CancellationToken cancellationToken);
    Task<TenantContract?> GetTenantContractForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<Property?> GetPropertyForUpdateAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TenantContract>> ListActiveTenantContractsAsync(Guid propertyId, Guid? exceptContractId, CancellationToken cancellationToken);
    Task AddTenantContractAsync(TenantContract contract, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

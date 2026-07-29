using RealEstateManagement.Domain.Contracts;

namespace RealEstateManagement.Application.Contracts;

public interface ILandlordContractStore
{
    Task<IReadOnlyList<LandlordContractDto>> ListLandlordContractsAsync(ContractFilterQuery query, CancellationToken cancellationToken);
    Task<LandlordContractDto?> GetLandlordContractAsync(Guid id, CancellationToken cancellationToken);
    Task<LandlordContract?> GetLandlordContractForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> PropertyExistsAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<bool> ContractExistsForPropertyAsync(Guid propertyId, Guid? exceptContractId, CancellationToken cancellationToken);
    Task AddLandlordContractAsync(LandlordContract contract, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

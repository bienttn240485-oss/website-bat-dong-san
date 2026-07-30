using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Properties;

public interface IPropertyStore
{
    Task<IReadOnlyList<PropertySummaryDto>> ListPropertiesAsync(PropertyFilterQuery query, CancellationToken cancellationToken);
    Task<PropertyFilterOptionsDto> GetFilterOptionsAsync(PropertyFilterQuery query, CancellationToken cancellationToken);
    Task<PropertyDetailDto?> GetPropertyDetailAsync(Guid id, CancellationToken cancellationToken);
    Task<Property?> GetPropertyForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string normalizedCode, Guid? exceptPropertyId, CancellationToken cancellationToken);
    Task<bool> HasContractRelationshipsAsync(Guid propertyId, CancellationToken cancellationToken);
    Task ClearPropertyChildrenAsync(Guid propertyId, CancellationToken cancellationToken);
    Task AddPropertyAsync(Property property, CancellationToken cancellationToken);
    void DeleteProperty(Property property);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

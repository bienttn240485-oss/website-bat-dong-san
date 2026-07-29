using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Properties;

public interface IPropertyStore
{
    Task<IReadOnlyList<PropertySummaryDto>> ListPropertiesAsync(PropertyFilterQuery query, CancellationToken cancellationToken);
    Task<PropertyDetailDto?> GetPropertyDetailAsync(Guid id, CancellationToken cancellationToken);
    Task<Property?> GetPropertyForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string normalizedCode, Guid? exceptPropertyId, CancellationToken cancellationToken);
    Task AddPropertyAsync(Property property, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

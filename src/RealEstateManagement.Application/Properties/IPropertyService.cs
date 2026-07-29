namespace RealEstateManagement.Application.Properties;

public interface IPropertyService
{
    Task<IReadOnlyList<PropertySummaryDto>> ListPropertiesAsync(PropertyFilterQuery query, CancellationToken cancellationToken = default);
    Task<PropertyDetailDto?> GetPropertyDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PropertyCommandResult> CreatePropertyAsync(PropertyEditorCommand command, CancellationToken cancellationToken = default);
    Task<PropertyCommandResult> UpdatePropertyAsync(Guid propertyId, PropertyEditorCommand command, CancellationToken cancellationToken = default);
    Task<PropertyCommandResult> ChangeStatusAsync(PropertyStatusCommand command, CancellationToken cancellationToken = default);
}

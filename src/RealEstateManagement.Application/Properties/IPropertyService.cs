namespace RealEstateManagement.Application.Properties;

public interface IPropertyService
{
    Task<IReadOnlyList<PropertySummaryDto>> ListPropertiesAsync(PropertyFilterQuery query, CancellationToken cancellationToken = default);
    Task<PropertyDetailDto?> GetPropertyDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicPropertyCardDto>> ListPublicRentalsAsync(PublicPropertyFilterQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicPropertyCardDto>> ListPublicSalesAsync(PublicPropertyFilterQuery query, CancellationToken cancellationToken = default);
    Task<PublicPropertyDetailDto?> GetPublicRentalDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PublicPropertyDetailDto?> GetPublicSaleDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PropertyCommandResult> CreatePropertyAsync(PropertyEditorCommand command, CancellationToken cancellationToken = default);
    Task<PropertyCommandResult> UpdatePropertyAsync(Guid propertyId, PropertyEditorCommand command, CancellationToken cancellationToken = default);
    Task<PropertyCommandResult> ChangeStatusAsync(PropertyStatusCommand command, CancellationToken cancellationToken = default);
    Task<PropertyCommandResult> DeletePropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);
}

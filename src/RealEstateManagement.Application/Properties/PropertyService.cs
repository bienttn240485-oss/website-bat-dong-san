using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Properties;

public sealed class PropertyService(IPropertyStore store, ISystemClock clock) : IPropertyService
{
    public Task<IReadOnlyList<PropertySummaryDto>> ListPropertiesAsync(PropertyFilterQuery query, CancellationToken cancellationToken = default)
        => store.ListPropertiesAsync(NormalizeQuery(query), cancellationToken);

    public Task<PropertyDetailDto?> GetPropertyDetailAsync(Guid id, CancellationToken cancellationToken = default)
        => store.GetPropertyDetailAsync(id, cancellationToken);

    public async Task<PropertyCommandResult> CreatePropertyAsync(PropertyEditorCommand command, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateEditorAsync(command, null, cancellationToken);
        if (errors.Count > 0)
        {
            return PropertyCommandResult.Failure(errors);
        }

        var now = clock.UtcNow;
        var propertyId = Guid.NewGuid();
        var property = new Property(
            propertyId,
            command.Code,
            command.Project,
            command.Area,
            command.Type,
            command.AreaSize,
            command.Bathrooms,
            command.MonthlyPrice,
            command.SalePrice,
            command.Direction,
            command.LoanInfo,
            command.LegalStatus,
            command.FurniturePackage,
            command.Description,
            command.VideoUrl,
            command.Status,
            command.AvailableFromDate,
            command.Notes,
            now);

        ReplaceChildren(property, command, propertyId);

        await store.AddPropertyAsync(property, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return PropertyCommandResult.Success(property.Id);
    }

    public async Task<PropertyCommandResult> UpdatePropertyAsync(Guid propertyId, PropertyEditorCommand command, CancellationToken cancellationToken = default)
    {
        var property = await store.GetPropertyForUpdateAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return PropertyCommandResult.Failure(["Không tìm thấy căn hộ cần cập nhật."]);
        }

        var errors = await ValidateEditorAsync(command, propertyId, cancellationToken);
        if (errors.Count > 0)
        {
            return PropertyCommandResult.Failure(errors);
        }

        var now = clock.UtcNow;
        property.UpdateDetails(
            command.Code,
            command.Project,
            command.Area,
            command.Type,
            command.AreaSize,
            command.Bathrooms,
            command.MonthlyPrice,
            command.SalePrice,
            command.Direction,
            command.LoanInfo,
            command.LegalStatus,
            command.FurniturePackage,
            command.Description,
            command.VideoUrl,
            command.AvailableFromDate,
            command.Notes,
            now);
        property.ChangeStatus(command.Status, now);
        await store.ClearPropertyChildrenAsync(propertyId, cancellationToken);
        ReplaceChildren(property, command, propertyId);

        await store.SaveChangesAsync(cancellationToken);

        return PropertyCommandResult.Success(property.Id);
    }

    public async Task<PropertyCommandResult> ChangeStatusAsync(PropertyStatusCommand command, CancellationToken cancellationToken = default)
    {
        var property = await store.GetPropertyForUpdateAsync(command.PropertyId, cancellationToken);
        if (property is null)
        {
            return PropertyCommandResult.Failure(["Không tìm thấy căn hộ cần cập nhật trạng thái."]);
        }

        property.ChangeStatus(command.Status, clock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);

        return PropertyCommandResult.Success(property.Id);
    }

    public async Task<PropertyCommandResult> DeletePropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        var property = await store.GetPropertyForUpdateAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return PropertyCommandResult.Failure(["Không tìm thấy căn hộ cần xóa."]);
        }

        if (await store.HasContractRelationshipsAsync(propertyId, cancellationToken))
        {
            return PropertyCommandResult.Failure(["Không thể xóa căn hộ đang có hợp đồng chủ nhà hoặc hợp đồng thuê."]);
        }

        store.DeleteProperty(property);
        await store.SaveChangesAsync(cancellationToken);

        return PropertyCommandResult.Success(property.Id);
    }

    private async Task<List<string>> ValidateEditorAsync(PropertyEditorCommand command, Guid? propertyId, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        Required(command.Code, "Vui lòng nhập mã căn hộ.", errors);

        if (command.MonthlyPrice is < 0)
        {
            errors.Add("Giá thuê không được âm.");
        }

        if (command.SalePrice is < 0)
        {
            errors.Add("Giá bán không được âm.");
        }

        if (!string.IsNullOrWhiteSpace(command.Code)
            && await store.CodeExistsAsync(NormalizeCode(command.Code), propertyId, cancellationToken))
        {
            errors.Add("Mã căn hộ đã tồn tại.");
        }

        ValidateImages(command.Images, errors);
        ValidateFurniture(command.FurnitureItems, errors);

        return errors;
    }

    private static void ReplaceChildren(Property property, PropertyEditorCommand command, Guid propertyId)
    {
        property.ReplaceImages(command.Images.Select(image => new PropertyImage(Guid.NewGuid(), propertyId, image.Url, image.AltText, image.SortOrder, image.IsPrimary)));
        property.ReplaceFurnitureItems(command.FurnitureItems.Select(item => new PropertyFurnitureItem(Guid.NewGuid(), propertyId, item.Name, item.Quantity, item.Notes)));
        property.ReplaceAmenities(command.Amenities
            .Select(amenity => amenity.Trim())
            .Where(amenity => amenity.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(amenity => new PropertyAmenity(Guid.NewGuid(), propertyId, amenity)));
    }

    private static void ValidateImages(IReadOnlyList<PropertyImageCommand> images, List<string> errors)
    {
        if (images.Count(image => image.IsPrimary) > 1)
        {
            errors.Add("Mỗi căn hộ chỉ được có một ảnh đại diện.");
        }
    }

    private static void ValidateFurniture(IReadOnlyList<PropertyFurnitureItemCommand> furnitureItems, List<string> errors)
    {
        if (furnitureItems.Any(item => item.Quantity < 0))
        {
            errors.Add("Số lượng nội thất không được âm.");
        }
    }

    private static PropertyFilterQuery NormalizeQuery(PropertyFilterQuery query)
        => query with
        {
            Keyword = NormalizeOptional(query.Keyword),
            Area = NormalizeOptional(query.Area)
        };

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Required(string? value, string message, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(message);
        }
    }
}

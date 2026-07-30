using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Properties;

public sealed class PropertyService(IPropertyStore store, ISystemClock clock) : IPropertyService
{
    private static readonly PropertyStatus[] PublicRentalStatuses =
    [
        PropertyStatus.Available,
        PropertyStatus.SoonAvailable,
        PropertyStatus.Reserved
    ];

    public Task<IReadOnlyList<PropertySummaryDto>> ListPropertiesAsync(PropertyFilterQuery query, CancellationToken cancellationToken = default)
        => store.ListPropertiesAsync(NormalizeQuery(query), cancellationToken);

    public Task<PropertyDetailDto?> GetPropertyDetailAsync(Guid id, CancellationToken cancellationToken = default)
        => store.GetPropertyDetailAsync(id, cancellationToken);

    public async Task<IReadOnlyList<PublicPropertyCardDto>> ListPublicRentalsAsync(PublicPropertyFilterQuery query, CancellationToken cancellationToken = default)
    {
        var properties = await store.ListPropertiesAsync(ToRentalQuery(query), cancellationToken);
        var visible = properties
            .Where(property => property.MonthlyPrice is > 0)
            .Where(property => PublicRentalStatuses.Contains(property.Status));

        return SortCards(visible.Select(ToPublicCard), query.SortBy, priceSelector: card => card.MonthlyPrice).ToArray();
    }

    public async Task<IReadOnlyList<PublicPropertyCardDto>> ListPublicSalesAsync(PublicPropertyFilterQuery query, CancellationToken cancellationToken = default)
    {
        var properties = await store.ListPropertiesAsync(ToSaleQuery(query), cancellationToken);
        var visible = properties.Where(property => property.SalePrice is > 0);

        return SortCards(visible.Select(ToPublicCard), query.SortBy, priceSelector: card => card.SalePrice).ToArray();
    }

    public async Task<PublicPropertyDetailDto?> GetPublicRentalDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await store.GetPropertyDetailAsync(id, cancellationToken);
        if (property is null || property.MonthlyPrice is not > 0 || !PublicRentalStatuses.Contains(property.Status))
        {
            return null;
        }

        return ToPublicDetail(property);
    }

    public async Task<PublicPropertyDetailDto?> GetPublicSaleDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await store.GetPropertyDetailAsync(id, cancellationToken);
        if (property is null || property.SalePrice is not > 0)
        {
            return null;
        }

        return ToPublicDetail(property);
    }

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

    private static PropertyFilterQuery ToRentalQuery(PublicPropertyFilterQuery query)
        => new(
            NormalizeOptional(query.Keyword),
            query.Project,
            NormalizeOptional(query.Area),
            query.Type,
            query.Status,
            query.MinPrice,
            query.MaxPrice);

    private static PropertyFilterQuery ToSaleQuery(PublicPropertyFilterQuery query)
        => new(
            NormalizeOptional(query.Keyword),
            query.Project,
            NormalizeOptional(query.Area),
            query.Type,
            null,
            MinSalePrice: query.MinPrice,
            MaxSalePrice: query.MaxPrice,
            SalesOnly: true);

    private static IEnumerable<PublicPropertyCardDto> SortCards(
        IEnumerable<PublicPropertyCardDto> cards,
        string? sortBy,
        Func<PublicPropertyCardDto, long?> priceSelector)
        => sortBy switch
        {
            PublicPropertySortOptions.PriceAsc => cards.OrderBy(card => priceSelector(card) ?? long.MaxValue).ThenBy(card => card.MaskedCode),
            PublicPropertySortOptions.PriceDesc => cards.OrderByDescending(card => priceSelector(card) ?? 0).ThenBy(card => card.MaskedCode),
            PublicPropertySortOptions.Code => cards.OrderBy(card => card.MaskedCode),
            _ => cards.OrderByDescending(card => card.CreatedAtUtc).ThenBy(card => card.MaskedCode)
        };

    private static PublicPropertyCardDto ToPublicCard(PropertySummaryDto property)
        => new(
            property.Id,
            MaskCode(property.Code),
            property.Project,
            property.Area,
            property.Type,
            property.AreaSize,
            property.Bathrooms,
            property.MonthlyPrice,
            property.SalePrice,
            property.Status,
            property.AvailableFromDate,
            property.PrimaryImageUrl,
            property.CreatedAtUtc);

    private static PublicPropertyDetailDto ToPublicDetail(PropertyDetailDto property)
        => new(
            property.Id,
            MaskCode(property.Code),
            property.Project,
            property.Area,
            property.Type,
            property.AreaSize,
            property.Bathrooms,
            property.MonthlyPrice,
            property.SalePrice,
            property.Direction,
            property.LoanInfo,
            property.LegalStatus,
            property.FurniturePackage,
            property.Description,
            property.VideoUrl,
            property.Status,
            property.AvailableFromDate,
            property.Images.Select(image => new PublicPropertyImageDto(image.Url, image.AltText ?? MaskCode(property.Code), image.SortOrder, image.IsPrimary)).ToArray(),
            property.FurnitureItems,
            property.Amenities);

    private static string MaskCode(string code)
    {
        var trimmed = code.Trim().ToUpperInvariant();
        if (trimmed.Length <= 4)
        {
            return $"{trimmed[..1]}***";
        }

        var separatorIndex = trimmed.IndexOf('-');
        if (separatorIndex >= 0 && separatorIndex < trimmed.Length - 1)
        {
            return $"{trimmed[..(separatorIndex + 1)]}***{trimmed[^2..]}";
        }

        return $"{trimmed[..Math.Min(3, trimmed.Length)]}***{trimmed[^2..]}";
    }

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

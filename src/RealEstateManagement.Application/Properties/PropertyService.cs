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

    public async Task<IReadOnlyList<PropertySummaryDto>> ListPropertiesAsync(PropertyFilterQuery query, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeQuery(query);
        var properties = await store.ListPropertiesAsync(normalized with { Keyword = null }, cancellationToken);
        return ApplyAdminKeyword(properties, normalized.Keyword).ToArray();
    }

    public Task<PropertyDetailDto?> GetPropertyDetailAsync(Guid id, CancellationToken cancellationToken = default)
        => store.GetPropertyDetailAsync(id, cancellationToken);

    public async Task<IReadOnlyList<PublicPropertyCardDto>> ListPublicRentalsAsync(PublicPropertyFilterQuery query, CancellationToken cancellationToken = default)
    {
        var properties = await store.ListPropertiesAsync(ToRentalQuery(query), cancellationToken);
        var visible = properties
            .Where(property => property.MonthlyPrice is > 0)
            .Where(property => PublicRentalStatuses.Contains(property.Status));

        visible = ApplyPublicKeyword(visible, query.Keyword);
        return SortCards(visible.Select(ToPublicCard), query.SortBy, priceSelector: card => card.MonthlyPrice).ToArray();
    }

    public async Task<IReadOnlyList<PublicPropertyCardDto>> ListPublicSalesAsync(PublicPropertyFilterQuery query, CancellationToken cancellationToken = default)
    {
        var properties = await store.ListPropertiesAsync(ToSaleQuery(query), cancellationToken);
        var visible = properties.Where(property => property.SalePrice is > 0);

        visible = ApplyPublicKeyword(visible, query.Keyword);
        return SortCards(visible.Select(ToPublicCard), query.SortBy, priceSelector: card => card.SalePrice).ToArray();
    }

    public Task<PropertyFilterOptionsDto> GetPublicRentalFilterOptionsAsync(CancellationToken cancellationToken = default)
        => store.GetFilterOptionsAsync(new PropertyFilterQuery(Status: null, MinMonthlyPrice: 1), cancellationToken);

    public Task<PropertyFilterOptionsDto> GetPublicSaleFilterOptionsAsync(CancellationToken cancellationToken = default)
        => store.GetFilterOptionsAsync(new PropertyFilterQuery(MinSalePrice: 1, SalesOnly: true), cancellationToken);

    public Task<PropertyFilterOptionsDto> GetAdminFilterOptionsAsync(CancellationToken cancellationToken = default)
        => store.GetFilterOptionsAsync(new PropertyFilterQuery(), cancellationToken);

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
        property.ReplaceImages(command.Images.Select(image => new PropertyImage(Guid.NewGuid(), propertyId, NormalizeImageUrl(image.Url), image.AltText, image.SortOrder, image.IsPrimary)));
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
        foreach (var image in images)
        {
            var url = image.Url.Trim();
            if (url.Length > 500)
            {
                errors.Add("URL ảnh không được vượt quá 500 ký tự.");
                continue;
            }

            if (!IsSafeImageUrl(url))
            {
                errors.Add("URL ảnh phải là đường dẫn cục bộ bắt đầu bằng / hoặc URL HTTPS hợp lệ.");
            }
        }
    }

    private static string NormalizeImageUrl(string url) => url.Trim();

    private static bool IsSafeImageUrl(string url)
        => IsSafeLocalPath(url) || IsSafeHttpsUrl(url);

    private static bool IsSafeHttpsUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

    private static bool IsSafeLocalPath(string url)
        => url.StartsWith("/", StringComparison.Ordinal)
            && !url.StartsWith("//", StringComparison.Ordinal)
            && !url.Any(char.IsControl);

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
            null,
            query.Project,
            NormalizeOptional(query.Area),
            query.Type,
            query.Status,
            query.MinPrice,
            query.MaxPrice);

    private static PropertyFilterQuery ToSaleQuery(PublicPropertyFilterQuery query)
        => new(
            null,
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
            PublicPropertySortOptions.PriceAsc => cards.OrderBy(card => priceSelector(card) ?? long.MaxValue).ThenBy(card => card.PublicReferenceCode),
            PublicPropertySortOptions.PriceDesc => cards.OrderByDescending(card => priceSelector(card) ?? 0).ThenBy(card => card.PublicReferenceCode),
            PublicPropertySortOptions.Code => cards.OrderBy(card => card.PublicReferenceCode),
            _ => cards.OrderByDescending(card => card.CreatedAtUtc).ThenBy(card => card.PublicReferenceCode)
        };

    private static PublicPropertyCardDto ToPublicCard(PropertySummaryDto property)
        => new(
            property.Id,
            PropertyReferenceCode.FromInternalCode(property.Code),
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
            PropertyReferenceCode.FromInternalCode(property.Code),
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
            property.Images.Select(image => new PublicPropertyImageDto(image.Url, image.AltText ?? PropertyReferenceCode.FromInternalCode(property.Code), image.SortOrder, image.IsPrimary)).ToArray(),
            property.FurnitureItems,
            property.Amenities);

    private static IEnumerable<PropertySummaryDto> ApplyPublicKeyword(IEnumerable<PropertySummaryDto> properties, string? keyword)
    {
        var normalized = NormalizeOptional(keyword);
        if (normalized is null)
        {
            return properties;
        }

        return properties.Where(property =>
        {
            var referenceCode = PropertyReferenceCode.FromInternalCode(property.Code);
            return Contains(referenceCode, normalized)
                || Contains(property.Project?.ToString(), normalized)
                || Contains(ProjectLabel(property.Project), normalized)
                || Contains(property.Area, normalized)
                || Contains(TypeLabel(property.Type), normalized)
                || Contains(property.Direction, normalized)
                || Contains(property.FurniturePackage, normalized)
                || property.Amenities.Any(amenity => Contains(amenity, normalized));
        });
    }

    private static IEnumerable<PropertySummaryDto> ApplyAdminKeyword(IEnumerable<PropertySummaryDto> properties, string? keyword)
    {
        var normalized = NormalizeOptional(keyword);
        if (normalized is null)
        {
            return properties;
        }

        return properties.Where(property =>
            Contains(property.Code, normalized)
            || Contains(PropertyReferenceCode.FromInternalCode(property.Code), normalized)
            || Contains(ProjectLabel(property.Project), normalized)
            || Contains(property.Area, normalized)
            || Contains(TypeLabel(property.Type), normalized)
            || Contains(StatusLabel(property.Status), normalized)
            || Contains(property.Direction, normalized)
            || Contains(property.FurniturePackage, normalized)
            || property.Amenities.Any(amenity => Contains(amenity, normalized)));
    }

    private static string ProjectLabel(PropertyProject? project)
        => project switch
        {
            PropertyProject.VinhomesGrandPark => "Vinhomes Grand Park",
            PropertyProject.Origami => "Origami",
            PropertyProject.GloryHeights => "Glory Heights",
            PropertyProject.Beverly => "Beverly",
            PropertyProject.BeverlySolari => "Beverly Solari",
            PropertyProject.LumiereBoulevard => "Lumiere Boulevard",
            PropertyProject.TheRainbow => "The Rainbow",
            PropertyProject.Manhattan => "Manhattan",
            PropertyProject.ManhattanGlory => "Manhattan Glory",
            PropertyProject.MasteriCentrePoint => "Masteri Centre Point",
            PropertyProject.OpusOne => "Opus One",
            null => string.Empty,
            _ => project.ToString() ?? string.Empty
        };

    private static string TypeLabel(PropertyType type)
        => type switch
        {
            PropertyType.Studio => "Studio",
            PropertyType.OneBedroom => "1 phòng ngủ",
            PropertyType.OneBedroomPlus => "1 phòng ngủ+",
            PropertyType.TwoBedroom => "2 phòng ngủ",
            PropertyType.TwoBedroomPlus => "2 phòng ngủ+",
            PropertyType.TwoBedroomOneBathroom => "2 phòng ngủ, 1 WC",
            PropertyType.TwoBedroomTwoBathrooms => "2 phòng ngủ, 2 WC",
            PropertyType.ThreeBedroom => "3 phòng ngủ",
            PropertyType.ThreeBedroomTwoBathrooms => "3 phòng ngủ, 2 WC",
            PropertyType.ThreeBedroomPlus => "3 phòng ngủ+",
            _ => type.ToString()
        };

    private static string StatusLabel(PropertyStatus status)
        => status switch
        {
            PropertyStatus.Available => "Đang trống",
            PropertyStatus.Occupied => "Đã thuê",
            PropertyStatus.SoonAvailable => "Sắp trống",
            PropertyStatus.Reserved => "Đã giữ chỗ",
            _ => status.ToString()
        };

    private static bool Contains(string? value, string keyword)
        => value?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;

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

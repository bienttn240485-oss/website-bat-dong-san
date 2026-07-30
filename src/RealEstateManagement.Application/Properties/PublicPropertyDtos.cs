using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Properties;

public sealed record PublicPropertyFilterQuery(
    string? Keyword = null,
    PropertyProject? Project = null,
    string? Area = null,
    PropertyType? Type = null,
    PropertyStatus? Status = null,
    long? MinPrice = null,
    long? MaxPrice = null,
    string SortBy = PublicPropertySortOptions.Newest);

public static class PublicPropertySortOptions
{
    public const string Newest = "newest";
    public const string PriceAsc = "price-asc";
    public const string PriceDesc = "price-desc";
    public const string Code = "code";
}

public sealed record PublicPropertyCardDto(
    Guid Id,
    string MaskedCode,
    PropertyProject? Project,
    string Area,
    PropertyType Type,
    decimal? AreaSize,
    int? Bathrooms,
    long? MonthlyPrice,
    long? SalePrice,
    PropertyStatus Status,
    DateOnly? AvailableFromDate,
    string? PrimaryImageUrl,
    DateTimeOffset CreatedAtUtc);

public sealed record PublicPropertyImageDto(string Url, string AltText, int SortOrder, bool IsPrimary);

public sealed record PublicPropertyDetailDto(
    Guid Id,
    string MaskedCode,
    PropertyProject? Project,
    string Area,
    PropertyType Type,
    decimal? AreaSize,
    int? Bathrooms,
    long? MonthlyPrice,
    long? SalePrice,
    string? Direction,
    string? LoanInfo,
    string? LegalStatus,
    string? FurniturePackage,
    string? Description,
    string? VideoUrl,
    PropertyStatus Status,
    DateOnly? AvailableFromDate,
    IReadOnlyList<PublicPropertyImageDto> Images,
    IReadOnlyList<PropertyFurnitureItemDto> FurnitureItems,
    IReadOnlyList<PropertyAmenityDto> Amenities);

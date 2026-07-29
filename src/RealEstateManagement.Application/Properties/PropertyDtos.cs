using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Properties;

public sealed record PropertyFilterQuery(
    string? Keyword = null,
    PropertyProject? Project = null,
    string? Area = null,
    PropertyType? Type = null,
    PropertyStatus? Status = null,
    long? MinMonthlyPrice = null,
    long? MaxMonthlyPrice = null,
    long? MinSalePrice = null,
    long? MaxSalePrice = null,
    bool SalesOnly = false);

public sealed record PropertySummaryDto(
    Guid Id,
    string Code,
    PropertyProject? Project,
    string Area,
    PropertyType Type,
    decimal? AreaSize,
    int? Bathrooms,
    long? MonthlyPrice,
    long? SalePrice,
    PropertyStatus Status,
    DateOnly? AvailableFromDate,
    string? PrimaryImageUrl);

public sealed record PropertyImageDto(Guid Id, string Url, string? AltText, int SortOrder, bool IsPrimary);

public sealed record PropertyFurnitureItemDto(Guid Id, string Name, int Quantity, string? Notes);

public sealed record PropertyAmenityDto(Guid Id, string Name);

public sealed record PropertyDetailDto(
    Guid Id,
    string Code,
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
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<PropertyImageDto> Images,
    IReadOnlyList<PropertyFurnitureItemDto> FurnitureItems,
    IReadOnlyList<PropertyAmenityDto> Amenities);

using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Properties;

public sealed record PropertyImageCommand(string Url, string? AltText, int SortOrder, bool IsPrimary);

public sealed record PropertyFurnitureItemCommand(string Name, int Quantity, string? Notes);

public sealed record PropertyEditorCommand(
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
    IReadOnlyList<PropertyImageCommand> Images,
    IReadOnlyList<PropertyFurnitureItemCommand> FurnitureItems,
    IReadOnlyList<string> Amenities);

public sealed record PropertyStatusCommand(Guid PropertyId, PropertyStatus Status);

public sealed record PropertyCommandResult(bool Succeeded, Guid? PropertyId, IReadOnlyList<string> Errors)
{
    public static PropertyCommandResult Success(Guid propertyId) => new(true, propertyId, []);

    public static PropertyCommandResult Failure(IEnumerable<string> errors) => new(false, null, errors.ToArray());
}

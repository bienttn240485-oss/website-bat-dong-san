namespace RealEstateManagement.Domain.Properties;

public sealed class Property
{
    private readonly List<PropertyImage> _images = [];
    private readonly List<PropertyFurnitureItem> _furnitureItems = [];
    private readonly List<PropertyAmenity> _amenities = [];

    private Property()
    {
    }

    public Property(
        Guid id,
        string code,
        PropertyProject? project,
        string area,
        PropertyType type,
        decimal? areaSize,
        int? bathrooms,
        long? monthlyPrice,
        long? salePrice,
        string? direction,
        string? loanInfo,
        string? legalStatus,
        string? furniturePackage,
        string? description,
        string? videoUrl,
        PropertyStatus status,
        DateOnly? availableFromDate,
        string? notes,
        DateTimeOffset utcNow)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Property ID is required.", nameof(id));
        }

        Id = id;
        UpdateDetails(
            code,
            project,
            area,
            type,
            areaSize,
            bathrooms,
            monthlyPrice,
            salePrice,
            direction,
            loanInfo,
            legalStatus,
            furniturePackage,
            description,
            videoUrl,
            availableFromDate,
            notes,
            utcNow);
        Status = status;
        CreatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public PropertyProject? Project { get; private set; }
    public string Area { get; private set; } = string.Empty;
    public PropertyType Type { get; private set; }
    public decimal? AreaSize { get; private set; }
    public int? Bathrooms { get; private set; }
    public long? MonthlyPrice { get; private set; }
    public long? SalePrice { get; private set; }
    public string? Direction { get; private set; }
    public string? LoanInfo { get; private set; }
    public string? LegalStatus { get; private set; }
    public string? FurniturePackage { get; private set; }
    public string? Description { get; private set; }
    public string? VideoUrl { get; private set; }
    public PropertyStatus Status { get; private set; }
    public DateOnly? AvailableFromDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<PropertyImage> Images => _images;
    public IReadOnlyCollection<PropertyFurnitureItem> FurnitureItems => _furnitureItems;
    public IReadOnlyCollection<PropertyAmenity> Amenities => _amenities;

    public void UpdateDetails(
        string code,
        PropertyProject? project,
        string area,
        PropertyType type,
        decimal? areaSize,
        int? bathrooms,
        long? monthlyPrice,
        long? salePrice,
        string? direction,
        string? loanInfo,
        string? legalStatus,
        string? furniturePackage,
        string? description,
        string? videoUrl,
        DateOnly? availableFromDate,
        string? notes,
        DateTimeOffset utcNow)
    {
        Code = NormalizeCode(code);
        Project = project;
        Area = string.IsNullOrWhiteSpace(area) ? string.Empty : area.Trim();
        Type = type;
        AreaSize = areaSize;
        Bathrooms = bathrooms;
        MonthlyPrice = ValidateMoney(monthlyPrice, nameof(monthlyPrice));
        SalePrice = ValidateMoney(salePrice, nameof(salePrice));
        Direction = NormalizeOptional(direction);
        LoanInfo = NormalizeOptional(loanInfo);
        LegalStatus = NormalizeOptional(legalStatus);
        FurniturePackage = NormalizeOptional(furniturePackage);
        Description = NormalizeOptional(description);
        VideoUrl = NormalizeOptional(videoUrl);
        AvailableFromDate = availableFromDate;
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = utcNow;
    }

    public void ChangeStatus(PropertyStatus status, DateTimeOffset utcNow)
    {
        Status = status;
        UpdatedAtUtc = utcNow;
    }

    public void ReplaceImages(IEnumerable<PropertyImage> images)
    {
        _images.Clear();
        _images.AddRange(images.OrderBy(image => image.SortOrder));
    }

    public void ReplaceFurnitureItems(IEnumerable<PropertyFurnitureItem> furnitureItems)
    {
        _furnitureItems.Clear();
        _furnitureItems.AddRange(furnitureItems.OrderBy(item => item.Name));
    }

    public void ReplaceAmenities(IEnumerable<PropertyAmenity> amenities)
    {
        _amenities.Clear();
        _amenities.AddRange(amenities.OrderBy(amenity => amenity.Name));
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Property code is required.", nameof(code));
        }

        return code.Trim().ToUpperInvariant();
    }

    private static long? ValidateMoney(long? amount, string parameterName)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Money amount cannot be negative.");
        }

        return amount;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

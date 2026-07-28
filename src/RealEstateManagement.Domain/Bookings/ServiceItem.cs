namespace RealEstateManagement.Domain.Bookings;

public sealed class ServiceItem
{
    private ServiceItem()
    {
    }

    public ServiceItem(
        Guid id,
        string code,
        string name,
        string? description,
        string unitName,
        long unitPrice,
        bool isQuantityTracked,
        int? availableQuantity,
        bool isActive,
        int sortOrder,
        DateTimeOffset utcNow)
    {
        Id = id;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UnitName = unitName.Trim();
        UnitPrice = unitPrice;
        IsQuantityTracked = isQuantityTracked;
        AvailableQuantity = availableQuantity;
        IsActive = isActive;
        SortOrder = sortOrder;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string UnitName { get; private set; } = string.Empty;
    public long UnitPrice { get; private set; }
    public bool IsQuantityTracked { get; private set; }
    public int? AvailableQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string code,
        string name,
        string? description,
        string unitName,
        long unitPrice,
        bool isQuantityTracked,
        int? availableQuantity,
        bool isActive,
        int sortOrder,
        DateTimeOffset utcNow)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UnitName = unitName.Trim();
        UnitPrice = unitPrice;
        IsQuantityTracked = isQuantityTracked;
        AvailableQuantity = isQuantityTracked ? availableQuantity : null;
        IsActive = isActive;
        SortOrder = sortOrder;
        UpdatedAtUtc = utcNow;
    }

    public void SetActive(bool isActive, DateTimeOffset utcNow)
    {
        IsActive = isActive;
        UpdatedAtUtc = utcNow;
    }
}


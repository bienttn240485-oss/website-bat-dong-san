namespace RealEstateManagement.Domain.Properties;

public sealed class PropertyFurnitureItem
{
    private PropertyFurnitureItem()
    {
    }

    public PropertyFurnitureItem(Guid id, Guid propertyId, string name, int quantity, string? notes)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Furniture item ID is required.", nameof(id));
        }

        if (propertyId == Guid.Empty)
        {
            throw new ArgumentException("Property ID is required.", nameof(propertyId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Furniture item name is required.", nameof(name));
        }

        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        }

        Id = id;
        PropertyId = propertyId;
        Name = name.Trim();
        Quantity = quantity;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public string? Notes { get; private set; }
}

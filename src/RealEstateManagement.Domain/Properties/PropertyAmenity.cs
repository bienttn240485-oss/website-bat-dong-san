namespace RealEstateManagement.Domain.Properties;

public sealed class PropertyAmenity
{
    private PropertyAmenity()
    {
    }

    public PropertyAmenity(Guid id, Guid propertyId, string name)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Amenity ID is required.", nameof(id));
        }

        if (propertyId == Guid.Empty)
        {
            throw new ArgumentException("Property ID is required.", nameof(propertyId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Amenity name is required.", nameof(name));
        }

        Id = id;
        PropertyId = propertyId;
        Name = name.Trim();
    }

    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
}

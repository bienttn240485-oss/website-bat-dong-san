namespace RealEstateManagement.Domain.Properties;

public sealed class PropertyImage
{
    private PropertyImage()
    {
    }

    public PropertyImage(Guid id, Guid propertyId, string url, string? altText, int sortOrder, bool isPrimary)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Image ID is required.", nameof(id));
        }

        if (propertyId == Guid.Empty)
        {
            throw new ArgumentException("Property ID is required.", nameof(propertyId));
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Image URL is required.", nameof(url));
        }

        Id = id;
        PropertyId = propertyId;
        Url = url.Trim();
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
    }

    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
}

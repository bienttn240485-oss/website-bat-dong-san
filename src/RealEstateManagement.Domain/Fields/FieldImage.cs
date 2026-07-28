namespace RealEstateManagement.Domain.Fields;

public sealed class FieldImage
{
    private FieldImage()
    {
    }

    public FieldImage(Guid id, Guid fieldId, string storagePath, string? altText, int sortOrder, bool isCover, DateTimeOffset utcNow)
    {
        Id = id;
        FieldId = fieldId;
        StoragePath = storagePath.Trim();
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        SortOrder = sortOrder;
        IsCover = isCover;
        CreatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid FieldId { get; private set; }
    public Field Field { get; private set; } = null!;
    public string StoragePath { get; private set; } = string.Empty;
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsCover { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}


namespace RealEstateManagement.Domain.Leads;

public sealed class Lead
{
    private Lead()
    {
    }

    public Lead(
        Guid id,
        string name,
        string contact,
        Guid? propertyId,
        string? subject,
        string? message,
        string? language,
        DateTimeOffset utcNow)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Lead ID is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Lead name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(contact))
        {
            throw new ArgumentException("Lead contact is required.", nameof(contact));
        }

        Id = id;
        Name = name.Trim();
        Contact = contact.Trim();
        PropertyId = propertyId;
        Subject = NormalizeOptional(subject);
        Message = NormalizeOptional(message);
        Language = string.IsNullOrWhiteSpace(language) ? "vi" : language.Trim();
        Status = LeadStatus.New;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Contact { get; private set; } = string.Empty;
    public Guid? PropertyId { get; private set; }
    public string? Subject { get; private set; }
    public string? Message { get; private set; }
    public string Language { get; private set; } = "vi";
    public LeadStatus Status { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void ChangeStatus(LeadStatus status, DateTimeOffset utcNow)
    {
        Status = status;
        UpdatedAtUtc = utcNow;
    }

    public void AssignTo(Guid saleUserId, DateTimeOffset utcNow)
    {
        if (saleUserId == Guid.Empty)
        {
            throw new ArgumentException("Sale user ID is required.", nameof(saleUserId));
        }

        AssignedToUserId = saleUserId;
        UpdatedAtUtc = utcNow;
    }

    public void Unassign(DateTimeOffset utcNow)
    {
        AssignedToUserId = null;
        UpdatedAtUtc = utcNow;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

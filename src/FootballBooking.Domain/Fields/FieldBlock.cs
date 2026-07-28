namespace FootballBooking.Domain.Fields;

public sealed class FieldBlock
{
    private FieldBlock()
    {
    }

    public FieldBlock(
        Guid id,
        Guid fieldId,
        DateOnly blockDate,
        int startMinute,
        int endMinute,
        FieldBlockType blockType,
        string reason,
        Guid createdByUserId,
        DateTimeOffset utcNow)
    {
        Id = id;
        FieldId = fieldId;
        BlockDate = blockDate;
        StartMinute = startMinute;
        EndMinute = endMinute;
        BlockType = blockType;
        Reason = reason.Trim();
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid FieldId { get; private set; }
    public Field Field { get; private set; } = null!;
    public DateOnly BlockDate { get; private set; }
    public int StartMinute { get; private set; }
    public int EndMinute { get; private set; }
    public FieldBlockType BlockType { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

namespace FootballBooking.Domain.Fields;

public sealed class FieldOperatingHour
{
    private FieldOperatingHour()
    {
    }

    public FieldOperatingHour(Guid id, Guid fieldId, int dayOfWeek, bool isClosed, int? openMinute, int? closeMinute)
    {
        Id = id;
        FieldId = fieldId;
        DayOfWeek = dayOfWeek;
        IsClosed = isClosed;
        OpenMinute = openMinute;
        CloseMinute = closeMinute;
    }

    public Guid Id { get; private set; }
    public Guid FieldId { get; private set; }
    public Field Field { get; private set; } = null!;
    public int DayOfWeek { get; private set; }
    public bool IsClosed { get; private set; }
    public int? OpenMinute { get; private set; }
    public int? CloseMinute { get; private set; }
}

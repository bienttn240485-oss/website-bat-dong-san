namespace RealEstateManagement.Domain.Bookings;

public sealed class BookingServiceLine
{
    private BookingServiceLine()
    {
    }

    public BookingServiceLine(
        Guid id,
        Guid bookingId,
        Guid? serviceId,
        string serviceCodeSnapshot,
        string serviceNameSnapshot,
        string unitNameSnapshot,
        long unitPrice,
        int quantity,
        Guid? addedByUserId,
        DateTimeOffset utcNow)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng dịch vụ phải lớn hơn 0.");
        }

        Id = id;
        BookingId = bookingId;
        ServiceId = serviceId;
        ServiceCodeSnapshot = serviceCodeSnapshot.Trim().ToUpperInvariant();
        ServiceNameSnapshot = serviceNameSnapshot.Trim();
        UnitNameSnapshot = unitNameSnapshot.Trim();
        UnitPrice = unitPrice;
        Quantity = quantity;
        LineTotal = unitPrice * quantity;
        AddedByUserId = addedByUserId;
        CreatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Booking Booking { get; private set; } = null!;
    public Guid? ServiceId { get; private set; }
    public ServiceItem? Service { get; private set; }
    public string ServiceCodeSnapshot { get; private set; } = string.Empty;
    public string ServiceNameSnapshot { get; private set; } = string.Empty;
    public string UnitNameSnapshot { get; private set; } = string.Empty;
    public long UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public long LineTotal { get; private set; }
    public Guid? AddedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}


namespace RealEstateManagement.Domain.Bookings;

public sealed class PromoCodeUsage
{
    private PromoCodeUsage()
    {
    }

    public PromoCodeUsage(Guid id, Guid promoCodeId, Guid bookingId, string customerPhoneNormalized, long discountAmount, DateTimeOffset utcNow)
    {
        Id = id;
        PromoCodeId = promoCodeId;
        BookingId = bookingId;
        CustomerPhoneNormalized = customerPhoneNormalized.Trim();
        DiscountAmount = discountAmount;
        CreatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid PromoCodeId { get; private set; }
    public PromoCode PromoCode { get; private set; } = null!;
    public Guid BookingId { get; private set; }
    public Booking Booking { get; private set; } = null!;
    public string CustomerPhoneNormalized { get; private set; } = string.Empty;
    public long DiscountAmount { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}


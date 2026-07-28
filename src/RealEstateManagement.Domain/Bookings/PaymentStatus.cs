namespace RealEstateManagement.Domain.Bookings;

public enum PaymentStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    RefundPending = 4,
    PartiallyRefunded = 5,
    Refunded = 6,
    Failed = 7
}


namespace FootballBooking.Domain.Bookings;

public enum BookingStatus
{
    PendingPayment = 1,
    Confirmed = 2,
    CheckedIn = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6,
    NoShow = 7,
    Expired = 8
}

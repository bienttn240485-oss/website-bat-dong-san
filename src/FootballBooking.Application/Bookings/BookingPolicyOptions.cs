namespace FootballBooking.Application.Bookings;

public sealed class BookingPolicyOptions
{
    public int PublicCancellationHoursBeforeStart { get; set; } = 12;
    public int LateCancellationFeePercent { get; set; } = 0;
    public int NoShowGraceMinutes { get; set; } = 15;
}

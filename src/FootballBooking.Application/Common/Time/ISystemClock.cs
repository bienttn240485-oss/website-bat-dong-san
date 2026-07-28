namespace FootballBooking.Application.Common.Time;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}

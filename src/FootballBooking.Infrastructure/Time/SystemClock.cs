using FootballBooking.Application.Common.Time;

namespace FootballBooking.Infrastructure.Time;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

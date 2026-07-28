using RealEstateManagement.Application.Common.Time;

namespace RealEstateManagement.Infrastructure.Time;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}


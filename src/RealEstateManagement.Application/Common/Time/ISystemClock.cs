namespace RealEstateManagement.Application.Common.Time;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}


namespace RealEstateManagement.Application.Bookings;

public interface IBookingWriteLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(Guid fieldId, DateOnly bookingDate, TimeSpan timeout, CancellationToken cancellationToken);
}


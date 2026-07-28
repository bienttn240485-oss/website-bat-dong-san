using System.Collections.Concurrent;
using FootballBooking.Application.Bookings;

namespace FootballBooking.Infrastructure.Bookings;

public sealed class BookingWriteLock : IBookingWriteLock
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new();

    public async Task<IAsyncDisposable?> TryAcquireAsync(Guid fieldId, DateOnly bookingDate, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var key = $"{fieldId:N}:{bookingDate:yyyyMMdd}";
        var semaphore = locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        if (!await semaphore.WaitAsync(timeout, cancellationToken))
        {
            return null;
        }

        return new Releaser(semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}

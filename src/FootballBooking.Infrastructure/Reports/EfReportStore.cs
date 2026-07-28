using FootballBooking.Application.Reports;
using FootballBooking.Domain.Fields;
using FootballBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FootballBooking.Infrastructure.Reports;

public sealed class EfReportStore(ApplicationDbContext dbContext) : IReportStore
{
    public async Task<IReadOnlyList<ReportBookingRecordDto>> ListBookingsAsync(DateOnly fromDate, DateOnly endExclusive, CancellationToken cancellationToken)
    {
        var bookings = await dbContext.Bookings
            .AsNoTracking()
            .Include(booking => booking.Field)
            .Include(booking => booking.ServiceLines)
            .Where(booking => booking.BookingDate >= fromDate && booking.BookingDate < endExclusive)
            .OrderBy(booking => booking.BookingDate)
            .ThenBy(booking => booking.StartMinute)
            .ToListAsync(cancellationToken);

        return bookings
            .Select(booking => new ReportBookingRecordDto(
                booking.Id,
                booking.BookingCode,
                booking.Field.Name,
                booking.BookingDate,
                booking.StartMinute,
                booking.EndMinute,
                booking.CustomerName,
                booking.Status,
                booking.PaymentStatus,
                booking.CourtAmount,
                booking.ServiceAmount,
                booking.DiscountAmount,
                booking.CancellationFeeAmount,
                booking.TotalAmount,
                booking.PaidAmount,
                booking.RefundedAmount,
                booking.ServiceLines
                    .OrderBy(line => line.ServiceNameSnapshot)
                    .Select(line => new ReportServiceLineDto(line.ServiceNameSnapshot, line.UnitNameSnapshot, line.Quantity))
                    .ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyList<ReportFieldCapacityDto>> ListFieldCapacitiesAsync(CancellationToken cancellationToken)
    {
        var fields = await dbContext.Fields
            .AsNoTracking()
            .Include(field => field.OperatingHours)
            .Where(field => field.Status == FieldStatus.Active)
            .OrderBy(field => field.Name)
            .ToListAsync(cancellationToken);

        return fields
            .SelectMany(field => field.OperatingHours.Select(hour => new ReportFieldCapacityDto(
                field.Name,
                hour.DayOfWeek,
                hour.IsClosed,
                hour.OpenMinute,
                hour.CloseMinute)))
            .ToArray();
    }

    public async Task<IReadOnlyList<ReportFieldBlockDto>> ListFieldBlocksAsync(DateOnly fromDate, DateOnly endExclusive, CancellationToken cancellationToken)
    {
        var blocks = await dbContext.FieldBlocks
            .AsNoTracking()
            .Include(block => block.Field)
            .Where(block => block.BlockDate >= fromDate && block.BlockDate < endExclusive)
            .OrderBy(block => block.BlockDate)
            .ThenBy(block => block.StartMinute)
            .ToListAsync(cancellationToken);

        return blocks
            .Select(block => new ReportFieldBlockDto(block.Field.Name, block.BlockDate, block.StartMinute, block.EndMinute, block.Reason))
            .ToArray();
    }
}

using System.Text;
using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Reports;
using RealEstateManagement.Domain.Bookings;

namespace RealEstateManagement.Tests.Application;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task GetOwnerDashboardAsync_WhenBookingsHaveMixedStatuses_ExcludesCancelledAndNoShowFromRevenue()
    {
        var store = new InMemoryReportStore();
        store.Bookings.Add(CreateBooking("FB-1", BookingStatus.Completed, 500000, 400000, 0));
        store.Bookings.Add(CreateBooking("FB-2", BookingStatus.Cancelled, 300000, 300000, 0));
        store.Bookings.Add(CreateBooking("FB-3", BookingStatus.NoShow, 250000, 250000, 0));
        var service = new ReportService(store, new FixedClock());

        var dashboard = await service.GetOwnerDashboardAsync(CancellationToken.None);

        Assert.Contains(dashboard.Metrics, metric => metric.Label == "Doanh thu tháng" && metric.Value == "400.000 ₫");
        Assert.Contains(dashboard.Metrics, metric => metric.Label == "Hủy vÃ  không đến" && metric.Value is "66,7%" or "66.7%");
    }

    [Fact]
    public async Task GetStaffDashboardAsync_WhenTodayHasServiceLines_ReturnsPreparationSummary()
    {
        var store = new InMemoryReportStore();
        store.Bookings.Add(CreateBooking("FB-1", BookingStatus.Confirmed, 500000, 100000, 0, [new("Nước suối", "chai", 12)]));
        store.Bookings.Add(CreateBooking("FB-2", BookingStatus.CheckedIn, 400000, 0, 0, [new("Nước suối", "chai", 8)]));
        var service = new ReportService(store, new FixedClock());

        var dashboard = await service.GetStaffDashboardAsync(CancellationToken.None);

        var serviceLine = Assert.Single(dashboard.ServicePreparation);
        Assert.Equal("Nước suối", serviceLine.ServiceName);
        Assert.Equal(20, serviceLine.Quantity);
    }

    [Fact]
    public async Task ExportRevenueCsvAsync_WhenRequested_UsesUtf8BomAndVietnameseHeaders()
    {
        var store = new InMemoryReportStore();
        store.Bookings.Add(CreateBooking("FB-1", BookingStatus.Completed, 500000, 400000, 0));
        var service = new ReportService(store, new FixedClock());

        var bytes = await service.ExportRevenueCsvAsync(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31), CancellationToken.None);
        var content = Encoding.UTF8.GetString(bytes);

        Assert.StartsWith(Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble()), content);
        Assert.Contains("Mã đặt sân", content);
        Assert.Contains("ÄÃ£ hoÃ n thÃ nh", content);
        Assert.DoesNotContain("Completed", content);
    }

    private static ReportBookingRecordDto CreateBooking(
        string code,
        BookingStatus status,
        long totalAmount,
        long paidAmount,
        long refundedAmount,
        IReadOnlyList<ReportServiceLineDto>? serviceLines = null)
        => new(
            Guid.NewGuid(),
            code,
            "Sân 5A",
            new DateOnly(2026, 7, 26),
            1080,
            1140,
            "Nguyễn Minh Tuấn",
            status,
            PaymentStatus.PartiallyPaid,
            totalAmount,
            serviceLines?.Count > 0 ? 50000 : 0,
            0,
            0,
            totalAmount,
            paidAmount,
            refundedAmount,
            serviceLines ?? []);

    private sealed class InMemoryReportStore : IReportStore
    {
        public List<ReportBookingRecordDto> Bookings { get; } = [];

        public Task<IReadOnlyList<ReportBookingRecordDto>> ListBookingsAsync(DateOnly fromDate, DateOnly endExclusive, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ReportBookingRecordDto>>(Bookings
                .Where(booking => booking.BookingDate >= fromDate && booking.BookingDate < endExclusive)
                .ToArray());

        public Task<IReadOnlyList<ReportFieldCapacityDto>> ListFieldCapacitiesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ReportFieldCapacityDto>>([
                new("Sân 5A", 0, false, 360, 1380),
                new("Sân 5A", 1, false, 360, 1380),
                new("Sân 5A", 2, false, 360, 1380),
                new("Sân 5A", 3, false, 360, 1380),
                new("Sân 5A", 4, false, 360, 1380),
                new("Sân 5A", 5, false, 360, 1380),
                new("Sân 5A", 6, false, 360, 1380)
            ]);

        public Task<IReadOnlyList<ReportFieldBlockDto>> ListFieldBlocksAsync(DateOnly fromDate, DateOnly endExclusive, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ReportFieldBlockDto>>([]);
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => new(2026, 7, 26, 4, 0, 0, TimeSpan.Zero);
    }
}


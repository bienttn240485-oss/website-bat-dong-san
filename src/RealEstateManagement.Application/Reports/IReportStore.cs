namespace RealEstateManagement.Application.Reports;

public interface IReportStore
{
    Task<IReadOnlyList<ReportBookingRecordDto>> ListBookingsAsync(DateOnly fromDate, DateOnly endExclusive, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportFieldCapacityDto>> ListFieldCapacitiesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportFieldBlockDto>> ListFieldBlocksAsync(DateOnly fromDate, DateOnly endExclusive, CancellationToken cancellationToken);
}


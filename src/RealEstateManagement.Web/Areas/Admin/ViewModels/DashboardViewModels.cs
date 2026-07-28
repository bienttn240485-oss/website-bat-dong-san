using RealEstateManagement.Application.Reports;

namespace RealEstateManagement.Web.Areas.Admin.ViewModels;

public sealed record AdminDashboardViewModel(bool IsOwner, OwnerDashboardDto? OwnerDashboard, StaffDashboardDto? StaffDashboard);

public sealed record AdminReportsViewModel(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<SummaryMetricDto> Metrics,
    IReadOnlyList<FieldUtilizationDto> FieldUtilization,
    IReadOnlyList<PeakHourDto> PeakHours,
    IReadOnlyList<ReportBookingRecordDto> PaymentsDue);


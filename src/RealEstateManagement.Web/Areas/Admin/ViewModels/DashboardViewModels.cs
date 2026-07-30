using System.Text.Json;
using RealEstateManagement.Application.Dashboard;

namespace RealEstateManagement.Web.Areas.Admin.ViewModels;

public sealed record AdminDashboardViewModel(
    bool CanViewFinancials,
    IReadOnlyList<DashboardMetricViewModel> Metrics,
    DashboardFinancialSummaryViewModel Financial,
    IReadOnlyList<DashboardWarningViewModel> Warnings,
    IReadOnlyList<DashboardTimelineItemViewModel> Timeline,
    IReadOnlyList<DashboardChartViewModel> Charts)
{
    public static AdminDashboardViewModel FromSnapshot(DashboardSnapshotDto snapshot, bool canViewFinancials)
    {
        var overview = snapshot.Overview;
        var metrics = new[]
        {
            new DashboardMetricViewModel("Tổng căn hộ", overview.TotalProperties.ToString("N0"), "Toàn bộ dữ liệu đang quản lý", "info"),
            new DashboardMetricViewModel("Đang trống", overview.AvailableProperties.ToString("N0"), "Có thể tư vấn ngay", "success"),
            new DashboardMetricViewModel("Đã thuê", overview.OccupiedProperties.ToString("N0"), "Đang có khách thuê", "active"),
            new DashboardMetricViewModel("Sắp trống", overview.SoonAvailableProperties.ToString("N0"), "Cần theo dõi ngày bàn giao", "warning"),
            new DashboardMetricViewModel("Đã giữ chỗ", overview.ReservedProperties.ToString("N0"), "Đang chờ xử lý", "info"),
            new DashboardMetricViewModel("Hợp đồng chủ nhà", overview.TotalLandlordContracts.ToString("N0"), "Tổng hợp đồng đầu vào", "info"),
            new DashboardMetricViewModel("Hợp đồng thuê Active", overview.ActiveTenantContracts.ToString("N0"), "Không tính hủy hoặc hết hiệu lực", "active"),
            new DashboardMetricViewModel("Tổng Lead", overview.TotalLeads.ToString("N0"), $"{overview.NewLeads:N0} lead mới", "warning"),
            new DashboardMetricViewModel("Lead chưa phân công", overview.UnassignedLeads.ToString("N0"), "Cần giao cho Sale", "danger"),
            new DashboardMetricViewModel("Căn có giá bán", overview.PropertiesForSale.ToString("N0"), "Có thể hiển thị ở /sales", "success"),
            new DashboardMetricViewModel("Căn có giá thuê", overview.PropertiesForRent.ToString("N0"), "Có thể hiển thị ở /properties", "success")
        };

        var charts = new List<DashboardChartViewModel>
        {
            ToChart("gmv-chart", "GMV 12 tháng gần nhất", "Tính theo RentalPrice x TermMonths, không gồm hợp đồng đã hủy.", snapshot.Charts.GmvLast12Months, true),
            ToChart("contracts-chart", "Hợp đồng ký theo tháng", "Số hợp đồng khách thuê được ký trong 12 tháng gần nhất.", snapshot.Charts.ContractsSignedLast12Months, false),
            ToChart("property-status-chart", "Phân bố trạng thái căn hộ", "Theo trạng thái vận hành hiện tại.", snapshot.Charts.PropertyStatusDistribution, false),
            ToChart("lead-status-chart", "Phân bố trạng thái Lead", "Theo trạng thái chăm sóc khách hàng.", snapshot.Charts.LeadStatusDistribution, false)
        };

        return new AdminDashboardViewModel(
            canViewFinancials,
            metrics,
            new DashboardFinancialSummaryViewModel(
                FormatMoney(snapshot.Financial.MonthlyInputTotal),
                FormatMoney(snapshot.Financial.MonthlyRentTotal),
                FormatMoney(snapshot.Financial.MonthlySpread),
                snapshot.Financial.NegativeMarginProperties.ToString("N0"),
                FormatMoney(snapshot.Financial.AverageMonthlySpread),
                FormatMoney(snapshot.Financial.TenantContractGmvLast12Months)),
            snapshot.Warnings.Select(warning => new DashboardWarningViewModel(warning.Title, warning.Description, warning.Tone, warning.Link)).ToArray(),
            snapshot.Timeline.Select(item => new DashboardTimelineItemViewModel(
                EventLabel(item.EventType),
                item.PropertyCode,
                item.Date.ToString("dd/MM/yyyy"),
                item.Description,
                item.Link,
                item.Tone)).ToArray(),
            charts);
    }

    private static DashboardChartViewModel ToChart(string id, string title, string description, DashboardChartDto chart, bool isFinancial)
        => new(id, title, description, $"Biểu đồ {title.ToLowerInvariant()}", JsonSerializer.Serialize(chart), isFinancial);

    private static string FormatMoney(long amount) => amount.ToString("N0").Replace(",", ".") + " ₫";

    private static string EventLabel(string eventType)
        => eventType switch
        {
            "TenantContractExpiry" => "Hết hạn thuê",
            "LandlordContractExpiry" => "Hết hạn chủ nhà",
            "PropertyAvailableFromDate" => "Ngày trống",
            "DepositReturnDate" => "Hoàn cọc",
            _ => eventType
        };
}

public sealed record DashboardMetricViewModel(string Label, string Value, string Note, string Tone);

public sealed record DashboardFinancialSummaryViewModel(
    string MonthlyInputTotal,
    string MonthlyRentTotal,
    string MonthlySpread,
    string NegativeMarginProperties,
    string AverageMonthlySpread,
    string TenantContractGmvLast12Months);

public sealed record DashboardWarningViewModel(string Title, string Description, string Tone, string? Link);

public sealed record DashboardTimelineItemViewModel(string EventType, string PropertyCode, string DateText, string Description, string Link, string Tone);

public sealed record DashboardChartViewModel(string Id, string Title, string Description, string AriaLabel, string JsonPayload, bool IsFinancial);
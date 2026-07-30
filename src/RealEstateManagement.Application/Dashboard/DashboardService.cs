using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Dashboard;

public sealed class DashboardService(IDashboardStore store, ISystemClock clock) : IDashboardService
{
    private const int ExpiringWithinDays = 30;
    private const int OverdueNewLeadDays = 3;
    private const int TimelineMonths = 12;

    public async Task<DashboardSnapshotDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, BusinessTimeZone()).DateTime);
        var source = await store.GetDashboardSourceAsync(today, ExpiringWithinDays, cancellationToken);

        return new DashboardSnapshotDto(
            BuildOverview(source),
            BuildFinancial(source, today),
            BuildWarnings(source, today, ExpiringWithinDays),
            BuildTimeline(source, today),
            BuildCharts(source, today));
    }

    private static DashboardOverviewDto BuildOverview(DashboardSourceDto source)
        => new(
            source.Properties.Count,
            source.Properties.Count(property => property.Status == PropertyStatus.Available),
            source.Properties.Count(property => property.Status == PropertyStatus.Occupied),
            source.Properties.Count(property => property.Status == PropertyStatus.SoonAvailable),
            source.Properties.Count(property => property.Status == PropertyStatus.Reserved),
            source.LandlordContracts.Count,
            ActiveTenantContracts(source.TenantContracts).Count,
            source.Leads.Count,
            source.Leads.Count(lead => lead.Status == LeadStatus.New),
            source.Leads.Count(lead => lead.AssignedToUserId is null),
            source.Properties.Count(property => property.SalePrice is > 0),
            source.Properties.Count(property => property.MonthlyPrice is > 0));

    private static DashboardFinancialSummaryDto BuildFinancial(DashboardSourceDto source, DateOnly today)
    {
        var activeTenants = ActiveTenantContracts(source.TenantContracts);
        var landlordsByProperty = source.LandlordContracts
            .GroupBy(contract => contract.PropertyId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(contract => contract.SignedDate).First());
        var activeTenantsByProperty = activeTenants
            .GroupBy(contract => contract.PropertyId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(contract => contract.SignedDate).First());

        var monthlyInputTotal = source.LandlordContracts.Sum(contract => contract.InputPrice);
        var monthlyRentTotal = activeTenants.Sum(contract => contract.RentalPrice);
        var spreads = activeTenantsByProperty
            .Where(pair => landlordsByProperty.ContainsKey(pair.Key))
            .Select(pair => pair.Value.RentalPrice - landlordsByProperty[pair.Key].InputPrice)
            .ToArray();
        var gmvFrom = FirstDayOfMonth(today).AddMonths(-11);
        var gmvTo = FirstDayOfMonth(today).AddMonths(1);
        var gmv = source.TenantContracts
            .Where(contract => contract.Status != ContractStatus.Cancelled)
            .Where(contract => contract.SignedDate >= gmvFrom && contract.SignedDate < gmvTo)
            .Sum(contract => contract.RentalPrice * contract.TermMonths);

        return new DashboardFinancialSummaryDto(
            monthlyInputTotal,
            monthlyRentTotal,
            monthlyRentTotal - monthlyInputTotal,
            spreads.Count(spread => spread < 0),
            spreads.Length == 0 ? 0 : (long)Math.Round(spreads.Average()),
            gmv);
    }

    private static IReadOnlyList<DashboardWarningDto> BuildWarnings(DashboardSourceDto source, DateOnly today, int expiringWithinDays)
    {
        var warnings = new List<DashboardWarningDto>();
        var expiringUntil = today.AddDays(expiringWithinDays);
        var activeTenants = ActiveTenantContracts(source.TenantContracts);
        var activeTenantPropertyIds = activeTenants.Select(contract => contract.PropertyId).ToHashSet();
        var landlordPropertyIds = source.LandlordContracts.Select(contract => contract.PropertyId).ToHashSet();
        var landlordsByProperty = source.LandlordContracts
            .GroupBy(contract => contract.PropertyId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(contract => contract.SignedDate).First());
        var activeTenantsByProperty = activeTenants
            .GroupBy(contract => contract.PropertyId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(contract => contract.SignedDate).First());

        warnings.AddRange(source.LandlordContracts
            .Where(contract => contract.ExpiryDate >= today && contract.ExpiryDate <= expiringUntil)
            .Select(contract => new DashboardWarningDto(
                "LandlordContractExpiring",
                "Hợp đồng chủ nhà sắp hết hạn",
                $"Căn {contract.PropertyCode} hết hạn ngày {FormatDate(contract.ExpiryDate)}.",
                "warning",
                $"/admin/landlord-contracts/{contract.Id}")));

        warnings.AddRange(activeTenants
            .Where(contract => contract.ExpiryDate >= today && contract.ExpiryDate <= expiringUntil)
            .Select(contract => new DashboardWarningDto(
                "TenantContractExpiring",
                "Hợp đồng khách thuê sắp hết hạn",
                $"Căn {contract.PropertyCode} hết hạn ngày {FormatDate(contract.ExpiryDate)}.",
                "warning",
                $"/admin/tenant-contracts/{contract.Id}")));

        warnings.AddRange(activeTenants
            .Where(contract => contract.ExpiryDate < today)
            .Select(contract => new DashboardWarningDto(
                "ExpiredActiveTenantContract",
                "Hợp đồng đã hết hạn nhưng vẫn Active",
                $"Căn {contract.PropertyCode} đã hết hạn ngày {FormatDate(contract.ExpiryDate)}.",
                "danger",
                $"/admin/tenant-contracts/{contract.Id}")));

        foreach (var property in source.Properties)
        {
            var hasActiveTenant = activeTenantPropertyIds.Contains(property.Id);
            if (property.Status == PropertyStatus.Occupied && !hasActiveTenant)
            {
                warnings.Add(new DashboardWarningDto(
                    "OccupiedWithoutActiveTenant",
                    "Căn đã thuê nhưng thiếu hợp đồng Active",
                    $"Căn {property.Code} đang Occupied nhưng không có hợp đồng khách thuê Active.",
                    "danger",
                    $"/admin/properties/{property.Id}"));
            }

            if (property.Status == PropertyStatus.Available && hasActiveTenant)
            {
                warnings.Add(new DashboardWarningDto(
                    "AvailableWithActiveTenant",
                    "Căn đang trống nhưng có hợp đồng Active",
                    $"Căn {property.Code} đang Available nhưng có hợp đồng khách thuê Active.",
                    "danger",
                    $"/admin/properties/{property.Id}"));
            }

            if (!landlordPropertyIds.Contains(property.Id))
            {
                warnings.Add(new DashboardWarningDto(
                    "MissingLandlordContract",
                    "Căn thiếu hợp đồng chủ nhà",
                    $"Căn {property.Code} chưa có hợp đồng chủ nhà.",
                    "warning",
                    $"/admin/properties/{property.Id}"));
            }

            if (property.Status == PropertyStatus.SoonAvailable && property.AvailableFromDate is null)
            {
                warnings.Add(new DashboardWarningDto(
                    "SoonAvailableMissingDate",
                    "Căn sắp trống thiếu ngày bàn giao",
                    $"Căn {property.Code} đang SoonAvailable nhưng chưa có ngày có thể vào ở.",
                    "warning",
                    $"/admin/properties/{property.Id}"));
            }

            if (landlordsByProperty.TryGetValue(property.Id, out var landlord)
                && activeTenantsByProperty.TryGetValue(property.Id, out var tenant)
                && tenant.RentalPrice < landlord.InputPrice)
            {
                warnings.Add(new DashboardWarningDto(
                    "NegativeMargin",
                    "Giá ra thấp hơn giá vào",
                    $"Căn {property.Code} đang âm {FormatMoney(landlord.InputPrice - tenant.RentalPrice)}/tháng.",
                    "danger",
                    $"/admin/properties/{property.Id}"));
            }
        }

        var overdueLeadThreshold = today.AddDays(-OverdueNewLeadDays);
        warnings.AddRange(source.Leads
            .Where(lead => lead.Status == LeadStatus.New && DateOnly.FromDateTime(lead.CreatedAtUtc.UtcDateTime) <= overdueLeadThreshold)
            .Select(lead => new DashboardWarningDto(
                "OverdueNewLead",
                "Lead mới quá 3 ngày chưa liên hệ",
                $"Lead tạo ngày {FormatDate(DateOnly.FromDateTime(lead.CreatedAtUtc.UtcDateTime))} vẫn đang ở trạng thái Mới.",
                "warning",
                $"/admin/leads/{lead.Id}")));

        warnings.AddRange(source.Leads
            .Where(lead => lead.AssignedToUserId is null)
            .Select(lead => new DashboardWarningDto(
                "UnassignedLead",
                "Lead chưa phân công",
                "Có lead chưa được giao cho Sale phụ trách.",
                "info",
                $"/admin/leads/{lead.Id}")));

        return warnings.OrderBy(WarningRank).ThenBy(warning => warning.Title).ToArray();
    }

    private static IReadOnlyList<DashboardTimelineItemDto> BuildTimeline(DashboardSourceDto source, DateOnly today)
    {
        var until = today.AddMonths(TimelineMonths);
        var items = new List<DashboardTimelineItemDto>();

        items.AddRange(source.TenantContracts
            .Where(contract => contract.ExpiryDate >= today && contract.ExpiryDate <= until)
            .Select(contract => new DashboardTimelineItemDto(
                "TenantContractExpiry",
                contract.PropertyCode,
                contract.ExpiryDate,
                "Hợp đồng khách thuê hết hạn",
                $"/admin/tenant-contracts/{contract.Id}",
                "warning")));

        items.AddRange(source.LandlordContracts
            .Where(contract => contract.ExpiryDate >= today && contract.ExpiryDate <= until)
            .Select(contract => new DashboardTimelineItemDto(
                "LandlordContractExpiry",
                contract.PropertyCode,
                contract.ExpiryDate,
                "Hợp đồng chủ nhà hết hạn",
                $"/admin/landlord-contracts/{contract.Id}",
                "info")));

        items.AddRange(source.Properties
            .Where(property => property.AvailableFromDate is not null && property.AvailableFromDate >= today && property.AvailableFromDate <= until)
            .Select(property => new DashboardTimelineItemDto(
                "PropertyAvailableFromDate",
                property.Code,
                property.AvailableFromDate!.Value,
                "Căn có thể vào ở",
                $"/admin/properties/{property.Id}",
                "success")));

        items.AddRange(source.TenantContracts
            .Where(contract => contract.DepositReturnDate is not null && contract.DepositReturnDate >= today && contract.DepositReturnDate <= until)
            .Select(contract => new DashboardTimelineItemDto(
                "DepositReturnDate",
                contract.PropertyCode,
                contract.DepositReturnDate!.Value,
                "Dự kiến hoàn cọc",
                $"/admin/tenant-contracts/{contract.Id}",
                "neutral")));

        return items
            .OrderBy(item => item.Date)
            .ThenBy(item => item.PropertyCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static DashboardChartsDto BuildCharts(DashboardSourceDto source, DateOnly today)
    {
        var months = Last12Months(today);
        var gmvByMonth = months.ToDictionary(month => month, _ => 0L);
        var signedByMonth = months.ToDictionary(month => month, _ => 0L);

        foreach (var contract in source.TenantContracts.Where(contract => contract.Status != ContractStatus.Cancelled))
        {
            var signedMonth = FirstDayOfMonth(contract.SignedDate);
            if (!gmvByMonth.ContainsKey(signedMonth))
            {
                continue;
            }

            gmvByMonth[signedMonth] += contract.RentalPrice * contract.TermMonths;
            signedByMonth[signedMonth]++;
        }

        var monthLabels = months.Select(month => month.ToString("MM/yyyy")).ToArray();
        var propertyStatuses = Enum.GetValues<PropertyStatus>();
        var leadStatuses = Enum.GetValues<LeadStatus>();

        return new DashboardChartsDto(
            new DashboardChartDto(monthLabels, [new DashboardChartDatasetDto("GMV", "bar", months.Select(month => gmvByMonth[month]).ToArray(), "success")]),
            new DashboardChartDto(monthLabels, [new DashboardChartDatasetDto("Hợp đồng ký", "bar", months.Select(month => signedByMonth[month]).ToArray(), "info")]),
            new DashboardChartDto(propertyStatuses.Select(PropertyStatusLabel).ToArray(), [new DashboardChartDatasetDto("Căn hộ", "bar", propertyStatuses.Select(status => (long)source.Properties.Count(property => property.Status == status)).ToArray(), "active")]),
            new DashboardChartDto(leadStatuses.Select(LeadStatusLabel).ToArray(), [new DashboardChartDatasetDto("Lead", "bar", leadStatuses.Select(status => (long)source.Leads.Count(lead => lead.Status == status)).ToArray(), "warning")]));
    }

    private static IReadOnlyList<DashboardTenantContractSourceDto> ActiveTenantContracts(IReadOnlyList<DashboardTenantContractSourceDto> contracts)
        => contracts.Where(contract => contract.Status == ContractStatus.Active).ToArray();

    private static DateOnly FirstDayOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    private static IReadOnlyList<DateOnly> Last12Months(DateOnly today)
    {
        var start = FirstDayOfMonth(today).AddMonths(-11);
        return Enumerable.Range(0, 12).Select(start.AddMonths).ToArray();
    }

    private static int WarningRank(DashboardWarningDto warning)
        => warning.Tone switch
        {
            "danger" => 0,
            "warning" => 1,
            "info" => 2,
            _ => 3
        };

    private static string FormatDate(DateOnly date) => date.ToString("dd/MM/yyyy");

    private static string FormatMoney(long amount) => string.Format("{0:N0} ₫", amount).Replace(",", ".");

    private static string PropertyStatusLabel(PropertyStatus status)
        => status switch
        {
            PropertyStatus.Available => "Đang trống",
            PropertyStatus.Occupied => "Đã thuê",
            PropertyStatus.SoonAvailable => "Sắp trống",
            PropertyStatus.Reserved => "Đã giữ chỗ",
            _ => status.ToString()
        };

    private static string LeadStatusLabel(LeadStatus status)
        => status switch
        {
            LeadStatus.New => "Mới",
            LeadStatus.Contacted => "Đã liên hệ",
            LeadStatus.Viewing => "Đang xem căn",
            LeadStatus.Converted => "Đã chốt",
            LeadStatus.Lost => "Không thành công",
            _ => status.ToString()
        };

    private static TimeZoneInfo BusinessTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}

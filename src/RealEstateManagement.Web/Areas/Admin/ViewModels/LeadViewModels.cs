using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using RealEstateManagement.Application.Leads;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Web.Areas.Admin.ViewModels;

public sealed class LeadListViewModel
{
    public LeadFilterViewModel Filter { get; set; } = new();
    public IReadOnlyList<LeadListItemViewModel> Leads { get; set; } = [];
    public bool CanAssign { get; set; }

    [ValidateNever]
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];

    [ValidateNever]
    public IReadOnlyList<SelectListItem> AssignedUserOptions { get; set; } = [];

    [ValidateNever]
    public IReadOnlyList<SelectListItem> ProjectOptions { get; set; } = [];
}

public sealed class LeadFilterViewModel
{
    [Display(Name = "Từ khóa")]
    public string? Keyword { get; set; }

    [Display(Name = "Trạng thái")]
    public LeadStatus? Status { get; set; }

    [Display(Name = "Sale phụ trách")]
    public Guid? AssignedToUserId { get; set; }

    [Display(Name = "Căn hộ")]
    public Guid? PropertyId { get; set; }

    [Display(Name = "Dự án")]
    public PropertyProject? Project { get; set; }

    [Display(Name = "Phân khu")]
    public string? Area { get; set; }

    [Display(Name = "Ngôn ngữ")]
    public string? Language { get; set; }

    [Display(Name = "Từ ngày")]
    public DateOnly? CreatedFrom { get; set; }

    [Display(Name = "Đến ngày")]
    public DateOnly? CreatedTo { get; set; }

    [Display(Name = "Chưa phân công")]
    public bool UnassignedOnly { get; set; }

    public LeadFilterQuery ToQuery()
        => new(Status, PropertyId, AssignedToUserId, CreatedFrom, CreatedTo, Keyword, Project, Area, Language, UnassignedOnly, NewestFirst: true);
}

public sealed record LeadListItemViewModel(
    Guid Id,
    string Name,
    string Contact,
    string? PropertyCode,
    string? PropertyArea,
    string? Subject,
    string Language,
    string StatusLabel,
    string StatusTone,
    string? AssignedToDisplayName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed class LeadDetailViewModel
{
    public required LeadDto Lead { get; init; }
    public required LeadStatusFormViewModel StatusForm { get; init; }
    public required LeadAssignmentFormViewModel AssignmentForm { get; init; }
    public LeadPropertySummaryViewModel? Property { get; init; }
    public bool CanAssign { get; init; }
    public bool CanUpdateStatus { get; init; }
    public IReadOnlyList<SelectListItem> StatusOptions { get; init; } = [];
    public IReadOnlyList<SelectListItem> AssignedUserOptions { get; init; } = [];
}

public sealed class LeadStatusFormViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
    public LeadStatus Status { get; set; }
}

public sealed class LeadAssignmentFormViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn Sale phụ trách.")]
    public Guid SaleUserId { get; set; }
}

public sealed record LeadPropertySummaryViewModel(
    Guid Id,
    string Code,
    string? Area,
    string TypeLabel,
    string MonthlyPrice,
    string SalePrice,
    string StatusLabel);

public sealed record PropertyLeadSummaryViewModel(
    int TotalCount,
    int NewCount,
    IReadOnlyList<LeadDto> RecentLeads);

public static class LeadDisplay
{
    public static string StatusLabel(LeadStatus status)
        => status switch
        {
            LeadStatus.New => "Mới",
            LeadStatus.Contacted => "Đã liên hệ",
            LeadStatus.Viewing => "Đang xem căn",
            LeadStatus.Converted => "Đã chốt",
            LeadStatus.Lost => "Không thành công",
            _ => "Không xác định"
        };

    public static string StatusTone(LeadStatus status)
        => status switch
        {
            LeadStatus.New => "info",
            LeadStatus.Contacted => "warning",
            LeadStatus.Viewing => "neutral",
            LeadStatus.Converted => "success",
            LeadStatus.Lost => "danger",
            _ => "neutral"
        };

    public static IReadOnlyList<SelectListItem> StatusOptions(LeadStatus? selected = null)
        => Enum.GetValues<LeadStatus>()
            .Select(status => new SelectListItem(StatusLabel(status), status.ToString(), status == selected))
            .ToArray();

    public static string LanguageLabel(string? language)
        => string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "English" : "Tiếng Việt";

    public static string DateTimeText(DateTimeOffset value)
        => value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public static string ContactHref(string contact)
    {
        if (contact.Contains('@') && Uri.CheckHostName(contact.Split('@').LastOrDefault() ?? string.Empty) != UriHostNameType.Unknown)
        {
            return $"mailto:{Uri.EscapeDataString(contact)}";
        }

        var phone = new string(contact.Where(char.IsDigit).ToArray());
        if (phone.Length >= 8)
        {
            return $"https://wa.me/{Uri.EscapeDataString(phone)}";
        }

        return string.Empty;
    }
}

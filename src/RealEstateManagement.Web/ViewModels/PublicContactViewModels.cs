using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using RealEstateManagement.Application.Leads;
using RealEstateManagement.Application.Properties;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Web.ViewModels;

public sealed class PublicContactViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên của bạn.")]
    [StringLength(160, ErrorMessage = "Tên không được vượt quá 160 ký tự.")]
    [Display(Name = "Họ tên")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập thông tin liên hệ.")]
    [StringLength(160, ErrorMessage = "Thông tin liên hệ không được vượt quá 160 ký tự.")]
    [Display(Name = "Liên hệ")]
    public string Contact { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Chủ đề không được vượt quá 200 ký tự.")]
    [Display(Name = "Chủ đề")]
    public string? Subject { get; set; }

    [StringLength(4000, ErrorMessage = "Nội dung không được vượt quá 4000 ký tự.")]
    [Display(Name = "Tin nhắn")]
    public string? Message { get; set; }

    [StringLength(20)]
    public string? Language { get; set; } = "vi";

    public LeadCreateCommand ToCommand()
        => new(Name, Contact, null, Subject, Message, Language);
}

public sealed class PropertyInquiryViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên của bạn.")]
    [StringLength(160, ErrorMessage = "Tên không được vượt quá 160 ký tự.")]
    [Display(Name = "Họ tên")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập thông tin liên hệ.")]
    [StringLength(160, ErrorMessage = "Thông tin liên hệ không được vượt quá 160 ký tự.")]
    [Display(Name = "Liên hệ")]
    public string Contact { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "Nội dung không được vượt quá 4000 ký tự.")]
    [Display(Name = "Tin nhắn")]
    public string? Message { get; set; }

    [StringLength(20)]
    public string? Language { get; set; } = "vi";

    public LeadCreateCommand ToCommand(Guid propertyId, string subject)
        => new(Name, Contact, propertyId, subject, Message, Language);
}

public sealed class HomePageViewModel
{
    public IReadOnlyList<PublicPropertyCardViewModel> FeaturedRentals { get; init; } = [];
    public IReadOnlyList<PublicPropertyCardViewModel> FeaturedSales { get; init; } = [];
}

public sealed class PublicPropertyListViewModel
{
    public required PublicPropertyFilterViewModel Filter { get; init; }
    public IReadOnlyList<PublicPropertyCardViewModel> Properties { get; init; } = [];
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string FormAction { get; init; }
    public required string DetailPrefix { get; init; }
    public bool IsSaleMode { get; init; }
    [ValidateNever]
    public IReadOnlyList<SelectListItem> ProjectOptions { get; init; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> TypeOptions { get; init; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> StatusOptions { get; init; } = [];
}

public sealed class PublicPropertyFilterViewModel
{
    [Display(Name = "Từ khóa")]
    public string? Keyword { get; set; }

    [Display(Name = "Dự án")]
    public PropertyProject? Project { get; set; }

    [Display(Name = "Phân khu")]
    public string? Area { get; set; }

    [Display(Name = "Loại căn")]
    public PropertyType? Type { get; set; }

    [Display(Name = "Trạng thái")]
    public PropertyStatus? Status { get; set; }

    [Display(Name = "Giá từ")]
    public long? MinPrice { get; set; }

    [Display(Name = "Giá đến")]
    public long? MaxPrice { get; set; }

    [Display(Name = "Sắp xếp")]
    public string SortBy { get; set; } = PublicPropertySortOptions.Newest;

    public PublicPropertyFilterQuery ToQuery()
        => new(Keyword, Project, Area, Type, Status, MinPrice, MaxPrice, SortBy);
}

public sealed record PublicPropertyCardViewModel(
    Guid Id,
    string MaskedCode,
    string ProjectLabel,
    string Area,
    string TypeLabel,
    string AreaText,
    string BathroomText,
    string PriceText,
    string? SecondaryPriceText,
    string StatusLabel,
    string StatusTone,
    string? AvailableFromText,
    string ImageUrl,
    string DetailUrl);

public sealed class PublicPropertyDetailViewModel
{
    public required PublicPropertyDetailDto Property { get; init; }
    public required string Title { get; init; }
    public required string PriceText { get; init; }
    public required string CtaText { get; init; }
    public required string InquirySubject { get; init; }
    public required string BackUrl { get; init; }
    public required string BackLabel { get; init; }
    public required PropertyInquiryViewModel Inquiry { get; init; }
    public bool IsSaleMode { get; init; }
}

public static class PublicPropertyDisplay
{
    public const string FallbackImage = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1200&q=80";

    public static PublicPropertyCardViewModel ToRentalCard(PublicPropertyCardDto property)
        => ToCard(property, $"/properties/{property.Id}", MoneyPerMonth(property.MonthlyPrice), property.SalePrice is > 0 ? Money(property.SalePrice) : null);

    public static PublicPropertyCardViewModel ToSaleCard(PublicPropertyCardDto property)
        => ToCard(property, $"/sales/{property.Id}", Money(property.SalePrice), property.MonthlyPrice is > 0 ? MoneyPerMonth(property.MonthlyPrice) : null);

    public static string ProjectLabel(PropertyProject? project)
        => project switch
        {
            PropertyProject.VinhomesGrandPark => "Vinhomes Grand Park",
            PropertyProject.Origami => "Origami",
            PropertyProject.GloryHeights => "Glory Heights",
            PropertyProject.Beverly => "Beverly",
            PropertyProject.BeverlySolari => "Beverly Solari",
            PropertyProject.LumiereBoulevard => "Lumiere Boulevard",
            PropertyProject.TheRainbow => "The Rainbow",
            PropertyProject.Manhattan => "Manhattan",
            PropertyProject.ManhattanGlory => "Manhattan Glory",
            PropertyProject.MasteriCentrePoint => "Masteri Centre Point",
            PropertyProject.OpusOne => "Opus One",
            null => "Vinhomes Grand Park",
            _ => project.ToString() ?? "Vinhomes Grand Park"
        };

    public static string TypeLabel(PropertyType type)
        => type switch
        {
            PropertyType.Studio => "Studio",
            PropertyType.OneBedroom => "1 phòng ngủ",
            PropertyType.OneBedroomPlus => "1 phòng ngủ+",
            PropertyType.TwoBedroom => "2 phòng ngủ",
            PropertyType.TwoBedroomPlus => "2 phòng ngủ+",
            PropertyType.TwoBedroomOneBathroom => "2 phòng ngủ, 1 WC",
            PropertyType.TwoBedroomTwoBathrooms => "2 phòng ngủ, 2 WC",
            PropertyType.ThreeBedroom => "3 phòng ngủ",
            PropertyType.ThreeBedroomTwoBathrooms => "3 phòng ngủ, 2 WC",
            PropertyType.ThreeBedroomPlus => "3 phòng ngủ+",
            _ => type.ToString()
        };

    public static string StatusLabel(PropertyStatus status)
        => status switch
        {
            PropertyStatus.Available => "Đang trống",
            PropertyStatus.Occupied => "Đã thuê",
            PropertyStatus.SoonAvailable => "Sắp trống",
            PropertyStatus.Reserved => "Đã giữ chỗ",
            _ => "Không xác định"
        };

    public static string StatusTone(PropertyStatus status)
        => status switch
        {
            PropertyStatus.Available => "success",
            PropertyStatus.SoonAvailable => "warning",
            PropertyStatus.Reserved => "info",
            PropertyStatus.Occupied => "neutral",
            _ => "neutral"
        };

    public static string Money(long? amount)
        => amount is null ? "Liên hệ" : string.Format("{0:N0} ₫", amount).Replace(",", ".");

    public static string MoneyPerMonth(long? amount)
        => amount is null ? "Liên hệ" : $"{Money(amount)}/tháng";

    public static string AreaText(decimal? area)
        => area is null ? "Đang cập nhật" : $"{area:0.##} m²";

    public static string BathroomText(int? bathrooms)
        => bathrooms is null ? "Đang cập nhật" : $"{bathrooms} WC";

    public static string DateText(DateOnly? date)
        => date?.ToString("dd/MM/yyyy") ?? "Đang cập nhật";

    public static string ImageUrl(string? url)
        => IsSafeHttpUrl(url) || IsSafeLocalPath(url) ? url! : FallbackImage;

    public static string? SafeExternalUrl(string? url)
        => IsSafeHttpUrl(url) ? url : null;

    public static IReadOnlyList<SelectListItem> ProjectOptions(PropertyProject? selected = null)
        => Enum.GetValues<PropertyProject>()
            .Select(project => new SelectListItem(ProjectLabel(project), project.ToString(), project == selected))
            .Prepend(new SelectListItem("Tất cả dự án", string.Empty, selected is null))
            .ToArray();

    public static IReadOnlyList<SelectListItem> TypeOptions(PropertyType? selected = null)
        => Enum.GetValues<PropertyType>()
            .Select(type => new SelectListItem(TypeLabel(type), type.ToString(), type == selected))
            .Prepend(new SelectListItem("Tất cả loại căn", string.Empty, selected is null))
            .ToArray();

    public static IReadOnlyList<SelectListItem> RentalStatusOptions(PropertyStatus? selected = null)
        => new[] { PropertyStatus.Available, PropertyStatus.SoonAvailable, PropertyStatus.Reserved }
            .Select(status => new SelectListItem(StatusLabel(status), status.ToString(), status == selected))
            .Prepend(new SelectListItem("Tất cả trạng thái", string.Empty, selected is null))
            .ToArray();

    public static IReadOnlyList<SelectListItem> SortOptions(string selected)
        => new[]
        {
            new SelectListItem("Mới nhất", PublicPropertySortOptions.Newest, selected == PublicPropertySortOptions.Newest),
            new SelectListItem("Giá thấp đến cao", PublicPropertySortOptions.PriceAsc, selected == PublicPropertySortOptions.PriceAsc),
            new SelectListItem("Giá cao đến thấp", PublicPropertySortOptions.PriceDesc, selected == PublicPropertySortOptions.PriceDesc),
            new SelectListItem("Theo mã căn", PublicPropertySortOptions.Code, selected == PublicPropertySortOptions.Code)
        };

    private static PublicPropertyCardViewModel ToCard(PublicPropertyCardDto property, string detailUrl, string priceText, string? secondaryPriceText)
        => new(
            property.Id,
            property.MaskedCode,
            ProjectLabel(property.Project),
            property.Area,
            TypeLabel(property.Type),
            AreaText(property.AreaSize),
            BathroomText(property.Bathrooms),
            priceText,
            secondaryPriceText,
            StatusLabel(property.Status),
            StatusTone(property.Status),
            property.Status == PropertyStatus.SoonAvailable ? DateText(property.AvailableFromDate) : null,
            ImageUrl(property.PrimaryImageUrl),
            detailUrl);

    private static bool IsSafeHttpUrl(string? url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https";

    private static bool IsSafeLocalPath(string? url)
        => !string.IsNullOrWhiteSpace(url) && url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal);
}

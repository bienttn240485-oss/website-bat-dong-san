using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
    [ValidateNever]
    public IReadOnlyList<SelectListItem> AreaOptions { get; init; } = [];
    [ValidateNever]
    public IReadOnlyList<PropertyAreaSuggestionViewModel> AreaSuggestions { get; init; } = [];
}

public sealed class PublicPropertyFilterViewModel : IValidatableObject
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
    public string? RentalPriceFromMillion { get; set; }

    [Display(Name = "Giá đến")]
    public string? RentalPriceToMillion { get; set; }

    [Display(Name = "Giá từ")]
    public string? SalePriceFromBillion { get; set; }

    [Display(Name = "Giá đến")]
    public string? SalePriceToBillion { get; set; }

    [Display(Name = "Sắp xếp")]
    public string SortBy { get; set; } = PublicPropertySortOptions.Newest;

    public PublicPropertyFilterQuery ToRentalQuery()
        => new(Keyword, Project, Area, Type, Status, ToVnd(RentalPriceFromMillion, 1_000_000L), ToVnd(RentalPriceToMillion, 1_000_000L), SortBy);

    public PublicPropertyFilterQuery ToSaleQuery()
        => new(Keyword, Project, Area, Type, null, ToVnd(SalePriceFromBillion, 1_000_000_000L), ToVnd(SalePriceToBillion, 1_000_000_000L), SortBy);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in ValidatePricePair(RentalPriceFromMillion, RentalPriceToMillion, 1_000_000L, "Giá thuê", nameof(RentalPriceFromMillion), nameof(RentalPriceToMillion)))
        {
            yield return error;
        }

        foreach (var error in ValidatePricePair(SalePriceFromBillion, SalePriceToBillion, 1_000_000_000L, "Giá bán", nameof(SalePriceFromBillion), nameof(SalePriceToBillion)))
        {
            yield return error;
        }
    }

    public string RentalPriceSummary()
        => BuildPriceSummary(RentalPriceFromMillion, RentalPriceToMillion, "Giá thuê", "triệu/tháng");

    public string SalePriceSummary()
        => BuildPriceSummary(SalePriceFromBillion, SalePriceToBillion, "Giá bán", "tỷ");

    public static long? ToVnd(string? input, long multiplier)
    {
        if (!TryParseDecimal(input, out var value) || value is null)
        {
            return null;
        }

        return checked((long)(value.Value * multiplier));
    }

    private static IEnumerable<ValidationResult> ValidatePricePair(string? from, string? to, long multiplier, string label, string fromMember, string toMember)
    {
        var hasFrom = !string.IsNullOrWhiteSpace(from);
        var hasTo = !string.IsNullOrWhiteSpace(to);
        var fromParsed = TryParseDecimal(from, out var fromValue);
        var toParsed = TryParseDecimal(to, out var toValue);

        if (hasFrom && !fromParsed)
        {
            yield return new ValidationResult($"{label} không hợp lệ.", [fromMember]);
        }

        if (hasTo && !toParsed)
        {
            yield return new ValidationResult($"{label} không hợp lệ.", [toMember]);
        }

        if (fromValue is < 0)
        {
            yield return new ValidationResult($"{label} không được âm.", [fromMember]);
        }

        if (toValue is < 0)
        {
            yield return new ValidationResult($"{label} không được âm.", [toMember]);
        }

        if (fromValue > decimal.Divide(long.MaxValue, multiplier) || toValue > decimal.Divide(long.MaxValue, multiplier))
        {
            yield return new ValidationResult($"{label} vượt quá phạm vi cho phép.", [fromMember, toMember]);
        }

        if (fromValue is not null && toValue is not null && fromValue > toValue)
        {
            yield return new ValidationResult("Giá từ không được lớn hơn giá đến.", [fromMember, toMember]);
        }
    }

    private static bool TryParseDecimal(string? input, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var normalized = input.Trim().Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        value = parsed;
        return true;
    }

    private static string BuildPriceSummary(string? from, string? to, string label, string unit)
    {
        _ = TryParseDecimal(from, out var fromValue);
        _ = TryParseDecimal(to, out var toValue);
        return (fromValue, toValue) switch
        {
            ({ } min, { } max) => $"{label} từ {FormatDecimal(min)} đến {FormatDecimal(max)} {unit}",
            ({ } min, null) => $"{label} từ {FormatDecimal(min)} {unit}",
            (null, { } max) => $"{label} đến {FormatDecimal(max)} {unit}",
            _ => string.Empty
        };
    }

    private static string FormatDecimal(decimal value)
        => value % 1 == 0 ? value.ToString("0", CultureInfo.InvariantCulture) : value.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');
}

public sealed record PropertyAreaSuggestionViewModel(string Project, string Area);

public sealed record PublicPropertyCardViewModel(
    Guid Id,
    string PublicReferenceCode,
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
    string ImageAltText,
    string DetailUrl);

public sealed record PublicPropertyImageViewModel(
    string ImageUrl,
    string AltText,
    string CssClass,
    string Loading = "lazy",
    int Width = 1200,
    int Height = 750);

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
    public const string FallbackImage = "/images/properties/property-placeholder.svg";

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
    {
        var trimmed = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        return IsSafeHttpUrl(trimmed) || IsSafeLocalPath(trimmed) ? trimmed! : FallbackImage;
    }

    public static string? SafeExternalUrl(string? url)
        => IsSafeHttpUrl(url) ? url : null;

    public static IReadOnlyList<SelectListItem> ProjectOptions(PropertyProject? selected = null, IEnumerable<PropertyProject>? projects = null)
        => (projects ?? Enum.GetValues<PropertyProject>())
            .Distinct()
            .OrderBy(project => ProjectLabel(project))
            .Select(project => new SelectListItem(ProjectLabel(project), project.ToString(), project == selected))
            .Prepend(new SelectListItem("Tất cả dự án", string.Empty, selected is null))
            .ToArray();

    public static IReadOnlyList<SelectListItem> TypeOptions(PropertyType? selected = null, IEnumerable<PropertyType>? types = null)
        => (types ?? Enum.GetValues<PropertyType>())
            .Distinct()
            .OrderBy(TypeLabel)
            .Select(type => new SelectListItem(TypeLabel(type), type.ToString(), type == selected))
            .Prepend(new SelectListItem("Tất cả loại căn", string.Empty, selected is null))
            .ToArray();

    public static IReadOnlyList<SelectListItem> RentalStatusOptions(PropertyStatus? selected = null, IEnumerable<PropertyStatus>? statuses = null)
        => (statuses ?? [PropertyStatus.Available, PropertyStatus.SoonAvailable, PropertyStatus.Reserved])
            .Where(status => status is PropertyStatus.Available or PropertyStatus.SoonAvailable or PropertyStatus.Reserved)
            .Distinct()
            .OrderBy(StatusLabel)
            .Select(status => new SelectListItem(StatusLabel(status), status.ToString(), status == selected))
            .Prepend(new SelectListItem("Tất cả trạng thái", string.Empty, selected is null))
            .ToArray();

    public static IReadOnlyList<SelectListItem> AreaOptions(IEnumerable<PropertyAreaOptionDto> areas, string? selected = null)
        => areas
            .Select(area => area.Area.Trim())
            .Where(area => area.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(area => area)
            .Select(area => new SelectListItem(area, area, string.Equals(area, selected, StringComparison.OrdinalIgnoreCase)))
            .Prepend(new SelectListItem("Tất cả phân khu", string.Empty, string.IsNullOrWhiteSpace(selected)))
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
            property.PublicReferenceCode,
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
            $"Căn hộ {ProjectLabel(property.Project)} tại {property.Area}",
            detailUrl);

    private static bool IsSafeHttpUrl(string? url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Scheme is "https";

    private static bool IsSafeLocalPath(string? url)
        => !string.IsNullOrWhiteSpace(url)
            && url.Length <= 500
            && url.StartsWith("/", StringComparison.Ordinal)
            && !url.StartsWith("//", StringComparison.Ordinal)
            && !url.Any(char.IsControl);
}

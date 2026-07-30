using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using RealEstateManagement.Application.Properties;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Web.Areas.Admin.ViewModels;

public sealed class PropertyListViewModel
{
    public PropertyFilterViewModel Filter { get; set; } = new();
    public IReadOnlyList<PropertyListItemViewModel> Properties { get; set; } = [];
    public bool CanManage { get; set; }
    public bool CanDelete { get; set; }
    [ValidateNever]
    public IReadOnlyList<SelectListItem> ProjectOptions { get; set; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> TypeOptions { get; set; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
}

public sealed class PropertyFilterViewModel
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

    [Display(Name = "Có giá bán")]
    public bool SalesOnly { get; set; }

    [Display(Name = "Sắp xếp")]
    public string SortBy { get; set; } = PropertySortOptions.Newest;

    public PropertyFilterQuery ToQuery()
        => new(Keyword, Project, Area, Type, Status, SalesOnly: SalesOnly);
}

public static class PropertySortOptions
{
    public const string Newest = "newest";
    public const string Code = "code";
}

public sealed record PropertyListItemViewModel(
    Guid Id,
    string Code,
    string ProjectLabel,
    string Area,
    string TypeLabel,
    decimal? AreaSize,
    long? MonthlyPrice,
    long? SalePrice,
    string StatusLabel,
    string StatusTone,
    DateOnly? AvailableFromDate);

public sealed class PropertyFormViewModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã căn hộ.")]
    [Display(Name = "Mã căn")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Dự án")]
    public PropertyProject? Project { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập phân khu.")]
    [Display(Name = "Phân khu")]
    public string Area { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn loại căn.")]
    [Display(Name = "Loại căn")]
    public PropertyType Type { get; set; } = PropertyType.TwoBedroomOneBathroom;

    [Display(Name = "Diện tích")]
    public decimal? AreaSize { get; set; }

    [Display(Name = "Số WC")]
    [Range(0, 100, ErrorMessage = "Số WC không được âm.")]
    public int? Bathrooms { get; set; }

    [Display(Name = "Giá thuê")]
    [Range(0, long.MaxValue, ErrorMessage = "Giá thuê không được âm.")]
    public long? MonthlyPrice { get; set; }

    [Display(Name = "Giá bán")]
    [Range(0, long.MaxValue, ErrorMessage = "Giá bán không được âm.")]
    public long? SalePrice { get; set; }

    [Display(Name = "Hướng")]
    public string? Direction { get; set; }

    [Display(Name = "Thông tin vay")]
    public string? LoanInfo { get; set; }

    [Display(Name = "Pháp lý")]
    public string? LegalStatus { get; set; }

    [Display(Name = "Gói nội thất")]
    public string? FurniturePackage { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Video URL")]
    public string? VideoUrl { get; set; }

    [Display(Name = "Trạng thái")]
    public PropertyStatus Status { get; set; } = PropertyStatus.Available;

    [Display(Name = "Ngày có thể vào ở")]
    public DateOnly? AvailableFromDate { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    [Display(Name = "Ảnh")]
    public string? ImagesText { get; set; }

    [Display(Name = "Nội thất")]
    public string? FurnitureText { get; set; }

    [Display(Name = "Tiện ích")]
    public string? AmenitiesText { get; set; }

    [ValidateNever]
    public IReadOnlyList<SelectListItem> ProjectOptions { get; set; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> TypeOptions { get; set; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AreaSize is <= 0)
        {
            yield return new ValidationResult("Diện tích phải lớn hơn 0.", [nameof(AreaSize)]);
        }

        if (Bathrooms is < 0)
        {
            yield return new ValidationResult("Số WC không được âm.", [nameof(Bathrooms)]);
        }

        if (MonthlyPrice is < 0)
        {
            yield return new ValidationResult("Giá thuê không được âm.", [nameof(MonthlyPrice)]);
        }

        if (SalePrice is < 0)
        {
            yield return new ValidationResult("Giá bán không được âm.", [nameof(SalePrice)]);
        }
    }

    public PropertyEditorCommand ToCommand()
        => new(
            Code,
            Project,
            Area,
            Type,
            AreaSize,
            Bathrooms,
            MonthlyPrice,
            SalePrice,
            Direction,
            LoanInfo,
            LegalStatus,
            FurniturePackage,
            Description,
            VideoUrl,
            Status,
            AvailableFromDate,
            Notes,
            ParseImages(ImagesText),
            ParseFurniture(FurnitureText),
            ParseAmenities(AmenitiesText));

    public static PropertyFormViewModel CreateDefault()
        => new()
        {
            Status = PropertyStatus.Available,
            Type = PropertyType.TwoBedroomOneBathroom,
            Project = PropertyProject.Origami,
            Area = string.Empty
        };

    public static PropertyFormViewModel FromDetail(PropertyDetailDto detail)
        => new()
        {
            Id = detail.Id,
            Code = detail.Code,
            Project = detail.Project,
            Area = detail.Area,
            Type = detail.Type,
            AreaSize = detail.AreaSize,
            Bathrooms = detail.Bathrooms,
            MonthlyPrice = detail.MonthlyPrice,
            SalePrice = detail.SalePrice,
            Direction = detail.Direction,
            LoanInfo = detail.LoanInfo,
            LegalStatus = detail.LegalStatus,
            FurniturePackage = detail.FurniturePackage,
            Description = detail.Description,
            VideoUrl = detail.VideoUrl,
            Status = detail.Status,
            AvailableFromDate = detail.AvailableFromDate,
            Notes = detail.Notes,
            ImagesText = string.Join(Environment.NewLine, detail.Images.OrderBy(image => image.SortOrder).Select(image => image.Url)),
            FurnitureText = string.Join(Environment.NewLine, detail.FurnitureItems.Select(item => $"{item.Name} | {item.Quantity} | {item.Notes}".TrimEnd(' ', '|'))),
            AmenitiesText = string.Join(Environment.NewLine, detail.Amenities.Select(amenity => amenity.Name))
        };

    private static IReadOnlyList<PropertyImageCommand> ParseImages(string? imagesText)
        => SplitLines(imagesText)
            .Select((url, index) => new PropertyImageCommand(url, $"Ảnh căn hộ {index + 1}", index + 1, index == 0))
            .ToArray();

    private static IReadOnlyList<PropertyFurnitureItemCommand> ParseFurniture(string? furnitureText)
        => SplitLines(furnitureText)
            .Select(line =>
            {
                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                var name = parts.ElementAtOrDefault(0) ?? string.Empty;
                var quantity = int.TryParse(parts.ElementAtOrDefault(1), out var parsedQuantity) ? parsedQuantity : 1;
                var notes = parts.ElementAtOrDefault(2);
                return new PropertyFurnitureItemCommand(name, quantity, string.IsNullOrWhiteSpace(notes) ? null : notes);
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .ToArray();

    private static IReadOnlyList<string> ParseAmenities(string? amenitiesText)
        => SplitLines(amenitiesText).ToArray();

    private static IEnumerable<string> SplitLines(string? text)
        => (text ?? string.Empty)
            .Split(["\r\n", "\n"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}

public sealed class PropertyDetailViewModel
{
    public required PropertyDetailDto Property { get; init; }
    public PropertyContractSummaryViewModel Contracts { get; init; } = new(null, null, [], null, null, []);
    public PropertyLeadSummaryViewModel Leads { get; init; } = new(0, 0, []);
    public bool CanManage { get; init; }
    public bool CanDelete { get; init; }
}

public sealed class PropertyDeleteViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

public static class PropertyDisplay
{
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
            null => "Chưa chọn",
            _ => project.ToString() ?? "Chưa chọn"
        };

    public static string TypeLabel(PropertyType type)
        => type switch
        {
            PropertyType.Studio => "Studio",
            PropertyType.OneBedroom => "1 phòng ngủ",
            PropertyType.OneBedroomPlus => "1 phòng ngủ + 1",
            PropertyType.TwoBedroom => "2 phòng ngủ",
            PropertyType.TwoBedroomPlus => "2 phòng ngủ + 1",
            PropertyType.TwoBedroomOneBathroom => "2 phòng ngủ, 1 WC",
            PropertyType.TwoBedroomTwoBathrooms => "2 phòng ngủ, 2 WC",
            PropertyType.ThreeBedroom => "3 phòng ngủ",
            PropertyType.ThreeBedroomTwoBathrooms => "3 phòng ngủ, 2 WC",
            PropertyType.ThreeBedroomPlus => "3 phòng ngủ + 1",
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
            PropertyStatus.Occupied => "neutral",
            PropertyStatus.SoonAvailable => "warning",
            PropertyStatus.Reserved => "info",
            _ => "neutral"
        };

    public static string FormatMoney(long? amount)
        => amount is null ? "Chưa có" : string.Format("{0:N0} ₫", amount).Replace(",", ".");

    public static string FormatArea(decimal? areaSize)
        => areaSize is null ? "Chưa có" : $"{areaSize:0.##} m²";

    public static IReadOnlyList<SelectListItem> ProjectOptions(PropertyProject? selected = null)
        => Enum.GetValues<PropertyProject>()
            .Select(project => new SelectListItem(ProjectLabel(project), project.ToString(), project == selected))
            .Prepend(new SelectListItem("Chưa chọn", string.Empty, selected is null))
            .ToArray();

    public static IReadOnlyList<SelectListItem> TypeOptions(PropertyType? selected = null)
        => Enum.GetValues<PropertyType>()
            .Select(type => new SelectListItem(TypeLabel(type), type.ToString(), type == selected))
            .ToArray();

    public static IReadOnlyList<SelectListItem> StatusOptions(PropertyStatus? selected = null)
        => Enum.GetValues<PropertyStatus>()
            .Select(status => new SelectListItem(StatusLabel(status), status.ToString(), status == selected))
            .ToArray();
}

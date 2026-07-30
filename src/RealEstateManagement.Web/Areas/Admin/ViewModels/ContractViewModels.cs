using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using RealEstateManagement.Application.Contracts;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Web.Areas.Admin.ViewModels;

public sealed class LandlordContractListViewModel
{
    public ContractFilterViewModel Filter { get; set; } = new();
    public IReadOnlyList<LandlordContractListItemViewModel> Contracts { get; set; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> ProjectOptions { get; set; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> DepositStatusOptions { get; set; } = [];
    public bool CanManage { get; set; }
    public bool CanDelete { get; set; }
}

public sealed class TenantContractListViewModel
{
    public ContractFilterViewModel Filter { get; set; } = new();
    public IReadOnlyList<TenantContractListItemViewModel> Contracts { get; set; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> ProjectOptions { get; set; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
    public bool CanManage { get; set; }
    public bool CanDelete { get; set; }
}

public sealed class ContractFilterViewModel
{
    [Display(Name = "Từ khóa")]
    public string? Keyword { get; set; }

    [Display(Name = "Căn hộ")]
    public Guid? PropertyId { get; set; }

    [Display(Name = "Dự án")]
    public PropertyProject? Project { get; set; }

    [Display(Name = "Phân khu")]
    public string? Area { get; set; }

    [Display(Name = "Trạng thái hợp đồng")]
    public ContractStatus? Status { get; set; }

    [Display(Name = "Trạng thái cọc")]
    public DepositStatus? DepositStatus { get; set; }

    [Display(Name = "Hết hạn trong 30 ngày")]
    public bool ExpiringSoon { get; set; }

    [Display(Name = "Đã hết hạn")]
    public bool ExpiredOnly { get; set; }

    [Display(Name = "Sắp xếp")]
    public string SortBy { get; set; } = ContractSortOptions.ExpiryDate;

    public ContractFilterQuery ToQuery(DateOnly today)
        => new(
            PropertyId,
            Keyword,
            Project,
            Area,
            Status,
            DepositStatus,
            ExpiringSoon ? today.AddDays(30) : ExpiredOnly ? today : null,
            ExpiredOnly);
}

public static class ContractSortOptions
{
    public const string ExpiryDate = "expiry-date";
    public const string SignedDate = "signed-date";
}

public sealed record LandlordContractListItemViewModel(
    Guid Id,
    Guid PropertyId,
    string PropertyCode,
    string ProjectLabel,
    string Area,
    string LandlordName,
    string? SaleName,
    string? PeCode,
    long InputPrice,
    DateOnly SignedDate,
    DateOnly ExpiryDate,
    DepositStatus DepositStatus,
    int? PaymentDay,
    DateOnly? NextDueDate,
    IReadOnlyList<ContractWarningViewModel> Warnings);

public sealed record TenantContractListItemViewModel(
    Guid Id,
    Guid PropertyId,
    string PropertyCode,
    string ProjectLabel,
    string Area,
    string TenantName,
    string? ManagerName,
    long RentalPrice,
    long DepositAmount,
    DateOnly SignedDate,
    DateOnly ExpiryDate,
    int TermMonths,
    ContractStatus Status,
    string? PeCode,
    IReadOnlyList<ContractWarningViewModel> Warnings);

public sealed class LandlordContractFormViewModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn căn hộ.")]
    [Display(Name = "Căn hộ")]
    public Guid PropertyId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên chủ nhà.")]
    [Display(Name = "Chủ nhà")]
    public string LandlordName { get; set; } = string.Empty;

    [Display(Name = "Mã PE")]
    public string? PeCode { get; set; }

    [Display(Name = "Sale phụ trách")]
    public string? SaleName { get; set; }

    [Range(0, long.MaxValue, ErrorMessage = "Giá vào không được âm.")]
    [Display(Name = "Giá nhập/tháng")]
    public long InputPrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập ngày ký.")]
    [Display(Name = "Ngày ký")]
    public DateOnly SignedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Ngày hết hạn")]
    public DateOnly? ExpiryDate { get; set; }

    [Display(Name = "Trạng thái bổ sung cọc")]
    public DepositStatus DepositStatus { get; set; } = DepositStatus.Pending;

    [Display(Name = "Ngày thanh toán")]
    public int? PaymentDay { get; set; }

    [Display(Name = "Cửa sổ thanh toán")]
    public string? PaymentWindow { get; set; }

    [Display(Name = "Kỳ thanh toán tiếp theo")]
    public DateOnly? NextDueDate { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    [ValidateNever]
    public IReadOnlyList<SelectListItem> PropertyOptions { get; set; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> DepositStatusOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ExpiryDate is not null && ExpiryDate <= SignedDate)
        {
            yield return new ValidationResult("Ngày hết hạn phải sau ngày ký.", [nameof(ExpiryDate)]);
        }

        if (PaymentDay is < 1 or > 31)
        {
            yield return new ValidationResult("Ngày thanh toán phải từ 1 đến 31.", [nameof(PaymentDay)]);
        }
    }

    public LandlordContractEditorCommand ToCommand()
        => new(PropertyId, LandlordName, PeCode, SaleName, InputPrice, SignedDate, ExpiryDate, DepositStatus, PaymentDay, PaymentWindow, NextDueDate, Notes);

    public static LandlordContractFormViewModel FromDto(LandlordContractDto dto)
        => new()
        {
            Id = dto.Id,
            PropertyId = dto.PropertyId,
            LandlordName = dto.LandlordName,
            PeCode = dto.PeCode,
            SaleName = dto.SaleName,
            InputPrice = dto.InputPrice,
            SignedDate = dto.SignedDate,
            ExpiryDate = dto.ExpiryDate,
            DepositStatus = dto.DepositStatus,
            PaymentDay = dto.PaymentDay,
            PaymentWindow = dto.PaymentWindow,
            NextDueDate = dto.NextDueDate,
            Notes = dto.Notes
        };
}

public sealed class TenantContractFormViewModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn căn hộ.")]
    [Display(Name = "Căn hộ")]
    public Guid PropertyId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên khách thuê.")]
    [Display(Name = "Khách thuê")]
    public string TenantName { get; set; } = string.Empty;

    [Display(Name = "Người phụ trách")]
    public string? ManagerName { get; set; }

    [Range(0, long.MaxValue, ErrorMessage = "Giá thuê không được âm.")]
    [Display(Name = "Giá thuê/tháng")]
    public long RentalPrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập ngày ký.")]
    [Display(Name = "Ngày ký")]
    public DateOnly SignedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(1, 120, ErrorMessage = "Thời hạn thuê phải lớn hơn 0 tháng.")]
    [Display(Name = "Thời hạn")]
    public int TermMonths { get; set; } = 12;

    [Range(0, long.MaxValue, ErrorMessage = "Tiền cọc không được âm.")]
    [Display(Name = "Tiền cọc")]
    public long DepositAmount { get; set; }

    [Display(Name = "Ngày trả cọc")]
    public DateOnly? DepositReturnDate { get; set; }

    [Display(Name = "Mã PE")]
    public string? PeCode { get; set; }

    [Display(Name = "Pass cửa")]
    public string? PassCode { get; set; }

    [Display(Name = "Trạng thái")]
    public ContractStatus Status { get; set; } = ContractStatus.Active;

    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    [ValidateNever]
    public IReadOnlyList<SelectListItem> PropertyOptions { get; set; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> TermOptions { get; set; } = [];
    [ValidateNever]
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];

    public DateOnly ExpiryDate => SignedDate.AddMonths(TermMonths);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TermMonths <= 0)
        {
            yield return new ValidationResult("Thời hạn thuê phải lớn hơn 0 tháng.", [nameof(TermMonths)]);
        }
    }

    public TenantContractEditorCommand ToCommand()
        => new(PropertyId, TenantName, ManagerName, RentalPrice, SignedDate, TermMonths, DepositAmount, DepositReturnDate, PeCode, PassCode, Status, Notes);

    public static TenantContractFormViewModel FromDto(TenantContractDto dto)
        => new()
        {
            Id = dto.Id,
            PropertyId = dto.PropertyId,
            TenantName = dto.TenantName,
            ManagerName = dto.ManagerName,
            RentalPrice = dto.RentalPrice,
            SignedDate = dto.SignedDate,
            TermMonths = dto.TermMonths,
            DepositAmount = dto.DepositAmount,
            DepositReturnDate = dto.DepositReturnDate,
            PeCode = dto.PeCode,
            PassCode = dto.PassCode,
            Status = dto.Status,
            Notes = dto.Notes
        };
}

public sealed record LandlordContractDetailViewModel(
    LandlordContractDto Contract,
    IReadOnlyList<ContractWarningViewModel> Warnings,
    bool CanManage,
    bool CanDelete);

public sealed record TenantContractDetailViewModel(
    TenantContractDto Contract,
    IReadOnlyList<ContractWarningViewModel> Warnings,
    bool CanManage,
    bool CanDelete);

public sealed record ContractWarningViewModel(string Message, string Tone);

public sealed record PropertyContractSummaryViewModel(
    LandlordContractDto? LandlordContract,
    TenantContractDto? ActiveTenantContract,
    IReadOnlyList<TenantContractDto> TenantContracts,
    long? MonthlyMargin,
    long? AnnualProjectedMargin,
    IReadOnlyList<ContractWarningViewModel> Warnings);

public static class ContractDisplay
{
    public static string DepositStatusLabel(DepositStatus status)
        => status switch
        {
            DepositStatus.NoExtension => "Không gia hạn cọc",
            DepositStatus.Supplemented => "Đã bổ sung",
            DepositStatus.Pending => "Chưa bổ sung",
            DepositStatus.NotApplicable => "Không áp dụng",
            _ => "Không xác định"
        };

    public static string ContractStatusLabel(ContractStatus status)
        => status switch
        {
            ContractStatus.Active => "Đang hiệu lực",
            ContractStatus.Expired => "Đã hết hạn",
            ContractStatus.Cancelled => "Đã hủy",
            ContractStatus.Renewed => "Đã gia hạn",
            _ => "Không xác định"
        };

    public static string ContractStatusTone(ContractStatus status)
        => status switch
        {
            ContractStatus.Active => "success",
            ContractStatus.Expired => "warning",
            ContractStatus.Cancelled => "danger",
            ContractStatus.Renewed => "info",
            _ => "neutral"
        };

    public static string DateText(DateOnly? date)
        => date?.ToString("dd/MM/yyyy") ?? "Chưa có";

    public static string Money(long? amount)
        => amount is null ? "Chưa có" : string.Format("{0:N0} ₫", amount).Replace(",", ".");

    public static IReadOnlyList<SelectListItem> DepositStatusOptions(DepositStatus? selected = null)
        => Enum.GetValues<DepositStatus>()
            .Select(status => new SelectListItem(DepositStatusLabel(status), status.ToString(), status == selected))
            .ToArray();

    public static IReadOnlyList<SelectListItem> ContractStatusOptions(ContractStatus? selected = null)
        => Enum.GetValues<ContractStatus>()
            .Select(status => new SelectListItem(ContractStatusLabel(status), status.ToString(), status == selected))
            .ToArray();

    public static IReadOnlyList<SelectListItem> TermOptions(int selected)
        => new[] { 3, 6, 12 }
            .Select(term => new SelectListItem($"{term} tháng", term.ToString(), term == selected))
            .ToArray();

    public static IReadOnlyList<ContractWarningViewModel> LandlordWarnings(LandlordContractDto contract, DateOnly today)
    {
        var warnings = new List<ContractWarningViewModel>();
        if (contract.ExpiryDate < today)
        {
            warnings.Add(new("Hợp đồng chủ nhà đã hết hạn.", "danger"));
        }
        else if (contract.ExpiryDate <= today.AddDays(30))
        {
            warnings.Add(new("Hợp đồng chủ nhà sắp hết hạn trong 30 ngày.", "warning"));
        }

        return warnings;
    }

    public static IReadOnlyList<ContractWarningViewModel> TenantWarnings(TenantContractDto contract, DateOnly today)
    {
        var warnings = new List<ContractWarningViewModel>();
        if (contract.Status == ContractStatus.Active && contract.ExpiryDate < today)
        {
            warnings.Add(new("Hợp đồng đã hết hạn nhưng vẫn đang ở trạng thái hiệu lực.", "danger"));
        }
        else if (contract.Status == ContractStatus.Active && contract.ExpiryDate <= today.AddDays(30))
        {
            warnings.Add(new("Hợp đồng khách thuê sắp hết hạn trong 30 ngày.", "warning"));
        }

        return warnings;
    }

    public static IReadOnlyList<ContractWarningViewModel> PropertyWarnings(
        PropertyStatus propertyStatus,
        LandlordContractDto? landlordContract,
        TenantContractDto? activeTenantContract,
        long? monthlyMargin,
        DateOnly today)
    {
        var warnings = new List<ContractWarningViewModel>();
        if (landlordContract is null)
        {
            warnings.Add(new("Căn hộ chưa có hợp đồng chủ nhà.", "warning"));
        }

        if (propertyStatus == PropertyStatus.Occupied && activeTenantContract is null)
        {
            warnings.Add(new("Căn hộ đang ở trạng thái đã thuê nhưng không có hợp đồng khách thuê đang hiệu lực.", "danger"));
        }

        if (propertyStatus == PropertyStatus.Available && activeTenantContract is not null)
        {
            warnings.Add(new("Căn hộ đang trống nhưng vẫn có hợp đồng khách thuê đang hiệu lực.", "danger"));
        }

        if (propertyStatus == PropertyStatus.Occupied && activeTenantContract is null)
        {
            warnings.Add(new("Căn hộ đã thuê nhưng thiếu hợp đồng khách thuê.", "warning"));
        }

        if (monthlyMargin is < 0)
        {
            warnings.Add(new("Giá ra đang thấp hơn giá vào.", "danger"));
        }

        if (landlordContract is not null)
        {
            warnings.AddRange(LandlordWarnings(landlordContract, today));
        }

        if (activeTenantContract is not null)
        {
            warnings.AddRange(TenantWarnings(activeTenantContract, today));
        }

        return warnings;
    }
}

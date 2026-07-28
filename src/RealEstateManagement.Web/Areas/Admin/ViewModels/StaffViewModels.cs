using System.ComponentModel.DataAnnotations;
using RealEstateManagement.Domain.Users;

namespace RealEstateManagement.Web.Areas.Admin.ViewModels;

public sealed record StaffListItemViewModel(
    Guid Id,
    string FullName,
    string Email,
    AccountStatus AccountStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc)
{
    public string StatusLabel => AccountStatus switch
    {
        AccountStatus.Active => "Đang hoạt động",
        AccountStatus.Locked => "Đã khóa",
        AccountStatus.Inactive => "Ngưng sử dụng",
        _ => "Không xác định"
    };

    public string StatusTone => AccountStatus == AccountStatus.Active ? "success" : "neutral";
}

public sealed class StaffFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ và tên nhân viên.")]
    [StringLength(120, ErrorMessage = "Họ và tên không được vượt quá 120 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    [Display(Name = "Mật khẩu tạm")]
    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;
}


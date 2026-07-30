using System.ComponentModel.DataAnnotations;
using RealEstateManagement.Application.Leads;

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
    [Display(Name = "Nội dung")]
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
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập thông tin liên hệ.")]
    [StringLength(160, ErrorMessage = "Thông tin liên hệ không được vượt quá 160 ký tự.")]
    public string Contact { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "Nội dung không được vượt quá 4000 ký tự.")]
    public string? Message { get; set; }

    [StringLength(20)]
    public string? Language { get; set; } = "vi";

    public LeadCreateCommand ToCommand(Guid propertyId)
        => new(Name, Contact, propertyId, "Yêu cầu tư vấn căn hộ", Message, Language);
}

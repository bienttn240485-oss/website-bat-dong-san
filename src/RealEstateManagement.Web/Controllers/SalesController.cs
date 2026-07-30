using Microsoft.AspNetCore.Mvc;
using RealEstateManagement.Application.Properties;
using RealEstateManagement.Web.ViewModels;

namespace RealEstateManagement.Web.Controllers;

public sealed class SalesController(IPropertyService propertyService) : Controller
{
    [HttpGet("/sales")]
    public async Task<IActionResult> Index([FromQuery] PublicPropertyFilterViewModel filter, CancellationToken cancellationToken)
    {
        filter.Status = null;
        var properties = await propertyService.ListPublicSalesAsync(filter.ToQuery(), cancellationToken);
        ViewData["Title"] = "Căn hộ bán";
        return View(new PublicPropertyListViewModel
        {
            Title = "Căn hộ bán",
            Subtitle = "Các căn hộ đang có giá bán, pháp lý và thông tin tư vấn rõ ràng.",
            FormAction = "/sales",
            DetailPrefix = "/sales",
            IsSaleMode = true,
            Filter = filter,
            Properties = properties.Select(PublicPropertyDisplay.ToSaleCard).ToArray(),
            ProjectOptions = PublicPropertyDisplay.ProjectOptions(filter.Project),
            TypeOptions = PublicPropertyDisplay.TypeOptions(filter.Type),
            StatusOptions = []
        });
    }

    [HttpGet("/sales/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var property = await propertyService.GetPublicSaleDetailAsync(id, cancellationToken);
        if (property is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Căn hộ {property.MaskedCode}";
        return View(new PublicPropertyDetailViewModel
        {
            Property = property,
            Title = "Thông tin căn hộ bán",
            PriceText = PublicPropertyDisplay.Money(property.SalePrice),
            CtaText = "Gửi yêu cầu mua căn này",
            InquirySubject = "Tư vấn mua căn",
            BackUrl = "/sales",
            BackLabel = "Quay lại căn bán",
            Inquiry = new PropertyInquiryViewModel(),
            IsSaleMode = true
        });
    }
}

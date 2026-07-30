using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using RealEstateManagement.Application.Properties;
using RealEstateManagement.Domain.Properties;
using RealEstateManagement.Web.ViewModels;

namespace RealEstateManagement.Web.Controllers;

public sealed class SalesController(IPropertyService propertyService) : Controller
{
    [HttpGet("/sales")]
    public async Task<IActionResult> Index([FromQuery] PublicPropertyFilterViewModel filter, CancellationToken cancellationToken)
    {
        filter.Status = null;
        ValidateFilter(filter);
        var filterOptions = await propertyService.GetPublicSaleFilterOptionsAsync(cancellationToken);
        var properties = ModelState.IsValid
            ? await propertyService.ListPublicSalesAsync(filter.ToSaleQuery(), cancellationToken)
            : [];

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
            ProjectOptions = PublicPropertyDisplay.ProjectOptions(filter.Project, filterOptions.Projects),
            TypeOptions = PublicPropertyDisplay.TypeOptions(filter.Type, filterOptions.Types),
            StatusOptions = [],
            AreaOptions = PublicPropertyDisplay.AreaOptions(VisibleAreas(filterOptions.Areas, filter.Project), filter.Area),
            AreaSuggestions = filterOptions.Areas.Select(area => new PropertyAreaSuggestionViewModel(area.Project?.ToString() ?? string.Empty, area.Area)).ToArray()
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

        ViewData["Title"] = $"Căn hộ {property.PublicReferenceCode}";
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

    private static IReadOnlyList<PropertyAreaOptionDto> VisibleAreas(
        IReadOnlyList<PropertyAreaOptionDto> areas,
        PropertyProject? selectedProject)
        => selectedProject is null
            ? areas
            : areas
                .Where(area => area.Project == selectedProject)
                .ToArray();

    private void ValidateFilter(PublicPropertyFilterViewModel filter)
    {
        foreach (var result in filter.Validate(new ValidationContext(filter)))
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Bộ lọc không hợp lệ.");
        }
    }
}

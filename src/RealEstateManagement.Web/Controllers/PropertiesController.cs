using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using RealEstateManagement.Application.Leads;
using RealEstateManagement.Application.Properties;
using RealEstateManagement.Domain.Properties;
using RealEstateManagement.Web.ViewModels;

namespace RealEstateManagement.Web.Controllers;

public sealed class PropertiesController(
    IPropertyService propertyService,
    ILeadService leadService) : Controller
{
    [HttpGet("/properties")]
    public async Task<IActionResult> Index([FromQuery] PublicPropertyFilterViewModel filter, CancellationToken cancellationToken)
    {
        ValidateFilter(filter);
        var filterOptions = await propertyService.GetPublicRentalFilterOptionsAsync(cancellationToken);
        var properties = ModelState.IsValid
            ? await propertyService.ListPublicRentalsAsync(filter.ToRentalQuery(), cancellationToken)
            : [];
        ViewData["Title"] = "Căn hộ cho thuê";
        return View(new PublicPropertyListViewModel
        {
            Title = "Căn hộ cho thuê",
            Subtitle = "Các căn hộ đang trống hoặc sắp trống tại Vinhomes Grand Park.",
            FormAction = "/properties",
            DetailPrefix = "/properties",
            IsSaleMode = false,
            Filter = filter,
            Properties = properties.Select(PublicPropertyDisplay.ToRentalCard).ToArray(),
            ProjectOptions = PublicPropertyDisplay.ProjectOptions(filter.Project, filterOptions.Projects),
            TypeOptions = PublicPropertyDisplay.TypeOptions(filter.Type, filterOptions.Types),
            StatusOptions = PublicPropertyDisplay.RentalStatusOptions(filter.Status, filterOptions.Statuses),
            AreaOptions = PublicPropertyDisplay.AreaOptions(VisibleAreas(filterOptions.Areas, filter.Project), filter.Area),
            AreaSuggestions = filterOptions.Areas.Select(area => new PropertyAreaSuggestionViewModel(area.Project?.ToString() ?? string.Empty, area.Area)).ToArray()
        });
    }

    [HttpGet("/properties/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var property = await propertyService.GetPublicRentalDetailAsync(id, cancellationToken);
        if (property is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Căn hộ {property.PublicReferenceCode}";
        return View(new PublicPropertyDetailViewModel
        {
            Property = property,
            Title = "Thông tin căn hộ cho thuê",
            PriceText = PublicPropertyDisplay.MoneyPerMonth(property.MonthlyPrice),
            CtaText = "Gửi yêu cầu thuê căn này",
            InquirySubject = "Tư vấn thuê căn",
            BackUrl = "/properties",
            BackLabel = "Quay lại căn cho thuê",
            Inquiry = new PropertyInquiryViewModel(),
            IsSaleMode = false
        });
    }

    [HttpPost("/properties/{propertyId:guid}/inquiry")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inquiry(Guid propertyId, PropertyInquiryViewModel model, string? intent, CancellationToken cancellationToken)
    {
        var isSaleIntent = string.Equals(intent, "sale", StringComparison.OrdinalIgnoreCase);

        if (!ModelState.IsValid)
        {
            var property = isSaleIntent
                ? await propertyService.GetPublicSaleDetailAsync(propertyId, cancellationToken)
                : await propertyService.GetPublicRentalDetailAsync(propertyId, cancellationToken);
            if (property is null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Căn hộ {property.PublicReferenceCode}";
            var viewModel = new PublicPropertyDetailViewModel
            {
                Property = property,
                Title = isSaleIntent ? "Thông tin căn hộ bán" : "Thông tin căn hộ cho thuê",
                PriceText = isSaleIntent ? PublicPropertyDisplay.Money(property.SalePrice) : PublicPropertyDisplay.MoneyPerMonth(property.MonthlyPrice),
                CtaText = isSaleIntent ? "Gửi yêu cầu mua căn này" : "Gửi yêu cầu thuê căn này",
                InquirySubject = isSaleIntent ? "Tư vấn mua căn" : "Tư vấn thuê căn",
                BackUrl = isSaleIntent ? "/sales" : "/properties",
                BackLabel = isSaleIntent ? "Quay lại căn bán" : "Quay lại căn cho thuê",
                Inquiry = model,
                IsSaleMode = isSaleIntent
            };
            return View(isSaleIntent ? "~/Views/Sales/Details.cshtml" : "Details", viewModel);
        }

        var propertyExists = isSaleIntent
            ? await propertyService.GetPublicSaleDetailAsync(propertyId, cancellationToken) is not null
            : await propertyService.GetPublicRentalDetailAsync(propertyId, cancellationToken) is not null;
        if (!propertyExists)
        {
            return NotFound();
        }

        var result = await leadService.CreateLeadAsync(model.ToCommand(propertyId, isSaleIntent ? "Tư vấn mua căn" : "Tư vấn thuê căn"), cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
            return Redirect(isSaleIntent ? $"/sales/{propertyId}" : $"/properties/{propertyId}");
        }

        TempData["SuccessMessage"] = "Đã gửi yêu cầu tư vấn căn hộ.";
        return Redirect(isSaleIntent ? $"/sales/{propertyId}" : $"/properties/{propertyId}");
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

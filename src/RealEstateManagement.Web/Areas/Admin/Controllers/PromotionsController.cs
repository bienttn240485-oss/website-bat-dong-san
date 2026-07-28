using RealEstateManagement.Application.Bookings;
using RealEstateManagement.Web.ViewModels.Bookings;
using RealEstateManagement.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstateManagement.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/promotions")]
[Authorize(Policy = "OwnerOnly")]
public sealed class PromotionsController(IBookingService bookingService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Khuyến mãi";
        ViewData["Breadcrumbs"] = new List<BreadcrumbItemViewModel>
        {
            new("Quản trị", "/admin/dashboard"),
            new("Khuyến mãi")
        };

        return View(new PromotionListViewModel
        {
            Promotions = await bookingService.ListAdminPromotionsAsync(cancellationToken)
        });
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        PrepareForm("Thêm khuyến mãi");
        return View("Form", new PromotionFormViewModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PromotionFormViewModel model, CancellationToken cancellationToken)
    {
        var command = BuildCommand(model);
        if (!ModelState.IsValid || command is null)
        {
            PrepareForm("Thêm khuyến mãi");
            return View("Form", model);
        }

        var result = await bookingService.SavePromotionAsync(command, cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result);
            PrepareForm("Thêm khuyến mãi");
            return View("Form", model);
        }

        TempData["SuccessMessage"] = "ÄÃ£ thêm khuyến mãi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var promotion = await bookingService.GetPromotionAsync(id, cancellationToken);
        if (promotion is null)
        {
            return NotFound();
        }

        PrepareForm("Cập nhật khuyến mãi");
        return View("Form", PromotionFormViewModel.FromDto(promotion));
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PromotionFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        var command = BuildCommand(model);
        if (!ModelState.IsValid || command is null)
        {
            PrepareForm("Cập nhật khuyến mãi");
            return View("Form", model);
        }

        var result = await bookingService.SavePromotionAsync(command, cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result);
            PrepareForm("Cập nhật khuyến mãi");
            return View("Form", model);
        }

        TempData["SuccessMessage"] = "ÄÃ£ cập nhật khuyến mãi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await bookingService.SetPromotionActiveAsync(id, isActive, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? (isActive ? "ÄÃ£ mở lại khuyến mãi." : "ÄÃ£ tạm dừng khuyến mãi.")
            : string.Join(" ", result.Errors);

        return RedirectToAction(nameof(Index));
    }

    private void PrepareForm(string title)
    {
        ViewData["Title"] = title;
        ViewData["Breadcrumbs"] = new List<BreadcrumbItemViewModel>
        {
            new("Quản trị", "/admin/dashboard"),
            new("Khuyến mãi", "/admin/promotions"),
            new(title)
        };
    }

    private PromoCodeUpsertCommand? BuildCommand(PromotionFormViewModel model)
    {
        var startsAt = model.ParseStartsAt();
        var endsAt = model.ParseEndsAt();
        if (startsAt is null)
        {
            ModelState.AddModelError(nameof(model.StartsAtText), "Thá»i gian bắt đầu chưa hợp lệ.");
        }

        if (endsAt is null)
        {
            ModelState.AddModelError(nameof(model.EndsAtText), "Thá»i gian kết thúc chưa hợp lệ.");
        }

        if (startsAt is null || endsAt is null)
        {
            return null;
        }

        return new PromoCodeUpsertCommand(
            model.Id,
            model.Code,
            model.Name,
            model.DiscountType,
            model.ToDiscountValue(),
            model.MaximumDiscountAmount,
            model.MinimumOrderAmount,
            startsAt.Value,
            endsAt.Value,
            model.TotalUsageLimit,
            model.PerPhoneUsageLimit,
            model.IsActive);
    }

    private void AddErrors(BookingCommandResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }
}


using System.Globalization;
using System.Security.Claims;
using RealEstateManagement.Application.Bookings;
using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Application.Fields;
using RealEstateManagement.Domain.Bookings;
using RealEstateManagement.Web.ViewModels.Bookings;
using RealEstateManagement.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstateManagement.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/bookings")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminOrSale)]
public sealed class BookingsController(IBookingService bookingService, IFieldService fieldService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? bookingDateText, Guid? fieldId, BookingStatus? status, CancellationToken cancellationToken)
    {
        var bookingDate = ParseDate(bookingDateText);
        var model = new AdminBookingListViewModel
        {
            BookingDateText = bookingDateText,
            FieldId = fieldId,
            Status = status,
            Fields = await fieldService.ListAdminFieldsAsync(cancellationToken),
            Bookings = await bookingService.ListAdminBookingsAsync(bookingDate, fieldId, status, cancellationToken)
        };

        PrepareBreadcrumb("Lá»‹ch Ä‘áº·t sÃ¢n");
        return View(model);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new BookingCreateViewModel
        {
            BookingDateText = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
        };
        await PopulateCreateModelAsync(model, cancellationToken);
        await PopulateAvailabilityAndQuoteAsync(model, cancellationToken);
        PrepareBreadcrumb("Táº¡o booking");
        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model, CancellationToken cancellationToken)
    {
        await PopulateCreateModelAsync(model, cancellationToken);
        var bookingDate = model.ParseBookingDate();
        if (bookingDate is null)
        {
            ModelState.AddModelError(nameof(model.BookingDateText), "NgÃƒÂ y Ä‘áº·t sÃ¢n cáº§n nháº­p theo Ä‘á»‹nh dáº¡ng dd/MM/yyyy.");
        }

        if (!ModelState.IsValid || bookingDate is null)
        {
            await PopulateAvailabilityAndQuoteAsync(model, cancellationToken);
            PrepareBreadcrumb("Táº¡o booking");
            return View(model);
        }

        var source = User.IsInRole(ApplicationRoles.Owner) ? BookingSource.Owner : BookingSource.Staff;
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdText, out var userId);

        var result = await bookingService.CreateBookingAsync(
            new BookingCreateCommand(
                model.FieldId,
                bookingDate.Value,
                model.StartMinute,
                model.EndMinute,
                model.CustomerName,
                model.CustomerPhone,
                model.CustomerEmail,
                model.Note,
                source,
                userId == Guid.Empty ? null : userId,
                SelectedServices(model),
                model.PromotionCode),
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await PopulateAvailabilityAndQuoteAsync(model, cancellationToken);
            PrepareBreadcrumb("Táº¡o booking");
            return View(model);
        }

        TempData["SuccessMessage"] = "Ã„ÂÃƒÂ£ táº¡o booking cho khÃ¡ch.";
        return RedirectToAction(nameof(Details), new { id = result.BookingId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var booking = await bookingService.GetAdminBookingAsync(id, cancellationToken);
        if (booking is null)
        {
            return NotFound();
        }

        PrepareBreadcrumb(booking.BookingCode);
        return View(new AdminBookingDetailViewModel { Booking = booking });
    }

    [HttpGet("{id:guid}/settlement")]
    public async Task<IActionResult> Settlement(Guid id, CancellationToken cancellationToken)
    {
        var booking = await bookingService.GetAdminBookingAsync(id, cancellationToken);
        if (booking is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Quyáº¿t toÃ¡n";
        ViewData["Breadcrumbs"] = new List<BreadcrumbItemViewModel>
        {
            new("Quáº£n trá»‹", "/admin/dashboard"),
            new("Lá»‹ch Ä‘áº·t sÃ¢n", "/admin/bookings"),
            new(booking.BookingCode, $"/admin/bookings/{booking.Id}"),
            new("Quyáº¿t toÃ¡n")
        };

        return View(new AdminBookingSettlementViewModel
        {
            Booking = booking,
            PaymentForm = new PaymentFormViewModel { Amount = booking.RemainingAmount > 0 ? booking.RemainingAmount : 0 }
        });
    }

    [HttpPost("{id:guid}/settlement")]
    [ValidateAntiForgeryToken]
    public IActionResult SettlementShortcut(Guid id)
        => RedirectToAction(nameof(Settlement), new { id });

    [HttpPost("{id:guid}/settlement/payments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordSettlementPayment(Guid id, PaymentFormViewModel model, CancellationToken cancellationToken)
    {
        var result = await RecordPaymentInternalAsync(id, model, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Ã„ÂÃƒÂ£ ghi nháº­n thanh toÃ¡n quyáº¿t toÃ¡n."
            : string.Join(" ", result.Errors);

        return result.Succeeded
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Settlement), new { id });
    }

    [HttpPost("{id:guid}/settlement/complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteFromSettlement(Guid id, CancellationToken cancellationToken)
    {
        var result = await bookingService.ChangeStatusAsync(id, BookingStatus.Completed, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Ã„ÂÃƒÂ£ xÃ¡c nháº­n hoÃƒÂ n thÃƒÂ nh lÆ°á»£t Ä‘áº·t sÃ¢n."
            : string.Join(" ", result.Errors);

        return RedirectToAction(nameof(Settlement), new { id });
    }

    [HttpPost("{id:guid}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(Guid id, BookingStatus targetStatus, CancellationToken cancellationToken)
        => await ChangeStatusAndRedirectAsync(id, targetStatus, cancellationToken);

    [HttpPost("{id:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
        => await ChangeStatusAndRedirectAsync(id, BookingStatus.Confirmed, cancellationToken);

    [HttpPost("{id:guid}/check-in")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken cancellationToken)
        => await ChangeStatusAndRedirectAsync(id, BookingStatus.CheckedIn, cancellationToken);

    [HttpPost("{id:guid}/start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
        => await ChangeStatusAndRedirectAsync(id, BookingStatus.InProgress, cancellationToken);

    [HttpPost("{id:guid}/complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
        => await ChangeStatusAndRedirectAsync(id, BookingStatus.Completed, cancellationToken);

    [HttpPost("{id:guid}/no-show")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NoShow(Guid id, CancellationToken cancellationToken)
        => await ChangeStatusAndRedirectAsync(id, BookingStatus.NoShow, cancellationToken);

    private async Task<IActionResult> ChangeStatusAndRedirectAsync(Guid id, BookingStatus targetStatus, CancellationToken cancellationToken)
    {
        var result = await bookingService.ChangeStatusAsync(id, targetStatus, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Ã„ÂÃƒÂ£ cáº­p nháº­t tráº¡ng thÃ¡i booking."
            : string.Join(" ", result.Errors);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/payments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(Guid id, PaymentFormViewModel model, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var result = await bookingService.RecordPaymentAsync(
            new BookingPaymentCommand(
                id,
                PaymentRecordType.Payment,
                model.Method,
                model.Amount,
                model.TransactionCode,
                model.Note,
                userId),
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Ã„ÂÃƒÂ£ ghi nháº­n thanh toÃ¡n cho booking. Sá»‘ Ä‘Ã£ thu vÃƒÂ  tráº¡ng thÃ¡i thanh toÃ¡n Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t."
            : string.Join(" ", result.Errors);

        return Redirect($"/admin/bookings/{id}#next-step");
    }

    private async Task<BookingCommandResult> RecordPaymentInternalAsync(Guid id, PaymentFormViewModel model, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        return await bookingService.RecordPaymentAsync(
            new BookingPaymentCommand(
                id,
                PaymentRecordType.Payment,
                model.Method,
                model.Amount,
                model.TransactionCode,
                model.Note,
                userId),
            cancellationToken);
    }

    [HttpPost("{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lÃ²ng nháº­p lÃ½ do há»§y booking.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await bookingService.CancelBookingAsync(new BookingCancellationCommand(id, model.Reason, CurrentUserId()), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Ã„ÂÃƒÂ£ há»§y booking."
            : string.Join(" ", result.Errors);

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateCreateModelAsync(BookingCreateViewModel model, CancellationToken cancellationToken)
    {
        model.Fields = await fieldService.ListPublicFieldsAsync(cancellationToken);
        await PopulateServicesAsync(model, cancellationToken);
        if (model.FieldId == Guid.Empty && model.Fields.Count > 0)
        {
            model.FieldId = model.Fields[0].Id;
        }
    }

    private async Task PopulateAvailabilityAndQuoteAsync(BookingCreateViewModel model, CancellationToken cancellationToken)
    {
        var bookingDate = model.ParseBookingDate();
        if (model.FieldId == Guid.Empty || bookingDate is null)
        {
            return;
        }

        model.Slots = await bookingService.GetAvailabilityAsync(model.FieldId, bookingDate.Value, cancellationToken);
        model.Quote = await bookingService.GetPricingQuoteAsync(model.FieldId, bookingDate.Value, model.StartMinute, model.EndMinute, cancellationToken);
    }

    private void PrepareBreadcrumb(string title)
    {
        ViewData["Title"] = title;
        ViewData["Breadcrumbs"] = new List<BreadcrumbItemViewModel>
        {
            new("Quáº£n trá»‹", "/admin/dashboard"),
            new("Lá»‹ch Ä‘áº·t sÃ¢n", "/admin/bookings"),
            new(title)
        };
    }

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private async Task PopulateServicesAsync(BookingCreateViewModel model, CancellationToken cancellationToken)
    {
        var currentQuantities = model.Services.ToDictionary(service => service.ServiceId, service => Math.Max(0, service.Quantity));
        var services = await bookingService.ListActiveServicesAsync(cancellationToken);
        model.Services = services
            .Select(service => new BookingServiceSelectionViewModel
            {
                ServiceId = service.Id,
                Name = service.Name,
                Description = service.Description,
                UnitName = service.UnitName,
                UnitPrice = service.UnitPrice,
                Quantity = currentQuantities.TryGetValue(service.Id, out var quantity) ? quantity : 0
            })
            .ToList();
    }

    private static IReadOnlyList<BookingServiceSelectionCommand> SelectedServices(BookingCreateViewModel model)
        => model.Services
            .Where(service => service.Quantity > 0)
            .Select(service => new BookingServiceSelectionCommand(service.ServiceId, service.Quantity))
            .ToArray();

    private Guid? CurrentUserId()
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdText, out var userId) && userId != Guid.Empty ? userId : null;
    }
}



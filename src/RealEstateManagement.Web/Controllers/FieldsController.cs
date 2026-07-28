using RealEstateManagement.Application.Fields;
using RealEstateManagement.Application.Bookings;
using RealEstateManagement.Web.ViewModels.Fields;
using Microsoft.AspNetCore.Mvc;

namespace RealEstateManagement.Web.Controllers;

public sealed class FieldsController(IFieldService fieldService, IBookingService bookingService) : Controller
{
    [HttpGet("/fields")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var fields = await fieldService.ListPublicFieldsAsync(cancellationToken);
        return View(new FieldListPageViewModel(fields));
    }

    [HttpGet("/fields/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var field = await fieldService.GetFieldDetailBySlugAsync(slug, cancellationToken);
        if (field is null)
        {
            return NotFound();
        }

        return View(new FieldDetailPageViewModel(field));
    }

    [HttpGet("/fields/{id:guid}/availability")]
    public async Task<IActionResult> Availability(Guid id, string date, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(date, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var bookingDate))
        {
            return BadRequest(new { message = "Ngày cần theo định dạng dd/MM/yyyy." });
        }

        var slots = await bookingService.GetAvailabilityAsync(id, bookingDate, cancellationToken);
        return Json(slots);
    }
}


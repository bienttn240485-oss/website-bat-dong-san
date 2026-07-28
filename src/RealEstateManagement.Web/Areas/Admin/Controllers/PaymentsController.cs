using RealEstateManagement.Application.Bookings;
using RealEstateManagement.Application.Fields;
using RealEstateManagement.Web.ViewModels.Bookings;
using RealEstateManagement.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstateManagement.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/payments")]
[Authorize(Policy = "InternalUser")]
public sealed class PaymentsController(IBookingService bookingService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Thanh toán";
        ViewData["Breadcrumbs"] = new List<BreadcrumbItemViewModel>
        {
            new("Quản trị", "/admin/dashboard"),
            new("Thanh toán")
        };

        return View(new PaymentListViewModel
        {
            Bookings = await bookingService.ListAdminBookingsAsync(null, null, null, cancellationToken)
        });
    }
}


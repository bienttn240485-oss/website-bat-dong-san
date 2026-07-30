using RealEstateManagement.Application.Bookings;
using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Application.Fields;
using RealEstateManagement.Web.ViewModels.Bookings;
using RealEstateManagement.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstateManagement.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/payments")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminOrSale)]
public sealed class PaymentsController(IBookingService bookingService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Thanh toÃ¡n";
        ViewData["Breadcrumbs"] = new List<BreadcrumbItemViewModel>
        {
            new("Quáº£n trá»‹", "/admin/dashboard"),
            new("Thanh toÃ¡n")
        };

        return View(new PaymentListViewModel
        {
            Bookings = await bookingService.ListAdminBookingsAsync(null, null, null, cancellationToken)
        });
    }
}




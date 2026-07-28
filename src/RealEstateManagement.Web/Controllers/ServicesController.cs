using RealEstateManagement.Application.Bookings;
using RealEstateManagement.Web.ViewModels.Bookings;
using Microsoft.AspNetCore.Mvc;

namespace RealEstateManagement.Web.Controllers;

[Route("services")]
public sealed class ServicesController(IBookingService bookingService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(new ServiceListViewModel
        {
            Services = await bookingService.ListActiveServicesAsync(cancellationToken)
        });
}


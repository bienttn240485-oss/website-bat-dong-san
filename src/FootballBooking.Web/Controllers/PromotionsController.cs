using FootballBooking.Application.Bookings;
using FootballBooking.Web.ViewModels.Bookings;
using Microsoft.AspNetCore.Mvc;

namespace FootballBooking.Web.Controllers;

[Route("promotions")]
public sealed class PromotionsController(IBookingService bookingService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(new PromotionListViewModel
        {
            Promotions = await bookingService.ListActivePromotionsAsync(cancellationToken)
        });
}

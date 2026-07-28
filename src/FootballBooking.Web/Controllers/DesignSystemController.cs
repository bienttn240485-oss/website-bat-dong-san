using Microsoft.AspNetCore.Mvc;

namespace FootballBooking.Web.Controllers;

[Route("dev/design-system")]
public sealed class DesignSystemController(IWebHostEnvironment environment) : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return View();
    }
}

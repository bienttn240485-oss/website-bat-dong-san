using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RealEstateManagement.Application.Leads;
using RealEstateManagement.Application.Properties;
using RealEstateManagement.Domain.Properties;
using RealEstateManagement.Web.Models;
using RealEstateManagement.Web.ViewModels;

namespace RealEstateManagement.Web.Controllers;

public class HomeController(
    IPropertyService propertyService,
    ILeadService leadService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var rentals = await propertyService.ListPublicRentalsAsync(new PublicPropertyFilterQuery(SortBy: PublicPropertySortOptions.Newest), cancellationToken);
        var sales = await propertyService.ListPublicSalesAsync(new PublicPropertyFilterQuery(SortBy: PublicPropertySortOptions.Newest), cancellationToken);

        ViewData["Title"] = "An Phú Real Estate";
        return View(new HomePageViewModel
        {
            FeaturedRentals = rentals.Take(3).Select(PublicPropertyDisplay.ToRentalCard).ToArray(),
            FeaturedSales = sales.Take(3).Select(PublicPropertyDisplay.ToSaleCard).ToArray()
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("/contact")]
    public IActionResult Contact()
    {
        ViewData["Title"] = "Liên hệ tư vấn";
        return View(new PublicContactViewModel());
    }

    [HttpPost("/contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(PublicContactViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Liên hệ tư vấn";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await leadService.CreateLeadAsync(model.ToCommand(), cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = "Cảm ơn bạn. Chúng tôi sẽ liên hệ lại trong thời gian sớm nhất.";
        return RedirectToAction(nameof(Contact));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

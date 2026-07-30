using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RealEstateManagement.Application.Leads;
using RealEstateManagement.Web.Models;
using RealEstateManagement.Web.ViewModels;

namespace RealEstateManagement.Web.Controllers;

public class HomeController(ILeadService leadService) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("/contact")]
    public IActionResult Contact()
    {
        return View(new PublicContactViewModel());
    }

    [HttpPost("/contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(PublicContactViewModel model, CancellationToken cancellationToken)
    {
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

using Microsoft.AspNetCore.Mvc;
using RealEstateManagement.Application.Leads;
using RealEstateManagement.Web.ViewModels;

namespace RealEstateManagement.Web.Controllers;

public sealed class PropertiesController(ILeadService leadService) : Controller
{
    [HttpPost("/properties/{propertyId:guid}/inquiry")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inquiry(Guid propertyId, PropertyInquiryViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin tư vấn.";
            return RedirectToAction("Contact", "Home");
        }

        var result = await leadService.CreateLeadAsync(model.ToCommand(propertyId), cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
            return RedirectToAction("Contact", "Home");
        }

        TempData["SuccessMessage"] = "Đã gửi yêu cầu tư vấn căn hộ.";
        return RedirectToAction("Contact", "Home");
    }
}

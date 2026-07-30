using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Contracts;
using RealEstateManagement.Application.Properties;
using RealEstateManagement.Web.Areas.Admin.ViewModels;
using RealEstateManagement.Web.ViewModels.Shared;

namespace RealEstateManagement.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/landlord-contracts")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminOrSale)]
public sealed class LandlordContractsController(
    ILandlordContractService landlordContractService,
    IPropertyService propertyService,
    ISystemClock clock) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] ContractFilterViewModel filter, CancellationToken cancellationToken)
    {
        PrepareView("Hợp đồng chủ nhà");
        var today = Today();
        var contracts = await landlordContractService.ListLandlordContractsAsync(filter.ToQuery(today), cancellationToken);

        var model = new LandlordContractListViewModel
        {
            Filter = filter,
            Contracts = contracts.Select(contract => new LandlordContractListItemViewModel(
                contract.Id,
                contract.PropertyId,
                contract.PropertyCode,
                PropertyDisplay.ProjectLabel(contract.Project),
                contract.Area,
                contract.LandlordName,
                contract.SaleName,
                contract.PeCode,
                contract.InputPrice,
                contract.SignedDate,
                contract.ExpiryDate,
                contract.DepositStatus,
                contract.PaymentDay,
                contract.NextDueDate,
                ContractDisplay.LandlordWarnings(contract, today))).ToArray(),
            ProjectOptions = PropertyDisplay.ProjectOptions(filter.Project),
            DepositStatusOptions = ContractDisplay.DepositStatusOptions(filter.DepositStatus),
            CanManage = CanManage(),
            CanDelete = CanDelete()
        };

        return View(model);
    }

    [HttpGet("create")]
    [Authorize(Policy = AuthorizationPolicies.CanManageContracts)]
    public async Task<IActionResult> Create(Guid? propertyId, CancellationToken cancellationToken)
    {
        PrepareView("Thêm hợp đồng chủ nhà");
        return View(await PrepareFormAsync(new LandlordContractFormViewModel { PropertyId = propertyId ?? Guid.Empty }, cancellationToken));
    }

    [HttpPost("create")]
    [Authorize(Policy = AuthorizationPolicies.CanManageContracts)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LandlordContractFormViewModel model, CancellationToken cancellationToken)
    {
        PrepareView("Thêm hợp đồng chủ nhà");
        if (!ModelState.IsValid)
        {
            return View(await PrepareFormAsync(model, cancellationToken));
        }

        var result = await landlordContractService.CreateLandlordContractAsync(model.ToCommand(), cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(await PrepareFormAsync(model, cancellationToken));
        }

        TempData["SuccessMessage"] = "Đã thêm hợp đồng chủ nhà.";
        return RedirectToAction(nameof(Details), new { id = result.ContractId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var contract = await landlordContractService.GetLandlordContractAsync(id, cancellationToken);
        if (contract is null)
        {
            return NotFound();
        }

        PrepareView($"Hợp đồng chủ nhà {contract.PropertyCode}");
        return View(new LandlordContractDetailViewModel(contract, ContractDisplay.LandlordWarnings(contract, Today()), CanManage(), CanDelete()));
    }

    [HttpGet("{id:guid}/edit")]
    [Authorize(Policy = AuthorizationPolicies.CanManageContracts)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var contract = await landlordContractService.GetLandlordContractAsync(id, cancellationToken);
        if (contract is null)
        {
            return NotFound();
        }

        PrepareView($"Cập nhật hợp đồng {contract.PropertyCode}");
        return View(await PrepareFormAsync(LandlordContractFormViewModel.FromDto(contract), cancellationToken));
    }

    [HttpPost("{id:guid}/edit")]
    [Authorize(Policy = AuthorizationPolicies.CanManageContracts)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, LandlordContractFormViewModel model, CancellationToken cancellationToken)
    {
        PrepareView("Cập nhật hợp đồng chủ nhà");
        if (!ModelState.IsValid)
        {
            model.Id = id;
            return View(await PrepareFormAsync(model, cancellationToken));
        }

        var result = await landlordContractService.UpdateLandlordContractAsync(id, model.ToCommand(), cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            model.Id = id;
            return View(await PrepareFormAsync(model, cancellationToken));
        }

        TempData["SuccessMessage"] = "Đã cập nhật hợp đồng chủ nhà.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/delete")]
    [Authorize(Policy = AuthorizationPolicies.CanManageContracts)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await landlordContractService.DeleteLandlordContractAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Đã xóa hợp đồng chủ nhà."
            : string.Join(" ", result.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<LandlordContractFormViewModel> PrepareFormAsync(LandlordContractFormViewModel model, CancellationToken cancellationToken)
    {
        model.PropertyOptions = await PropertyOptionsAsync(model.PropertyId, cancellationToken);
        model.DepositStatusOptions = ContractDisplay.DepositStatusOptions(model.DepositStatus);
        return model;
    }

    private async Task<IReadOnlyList<SelectListItem>> PropertyOptionsAsync(Guid selected, CancellationToken cancellationToken)
    {
        var properties = await propertyService.ListPropertiesAsync(new PropertyFilterQuery(), cancellationToken);
        return properties
            .OrderBy(property => property.Code)
            .Select(property => new SelectListItem($"{property.Code} - {property.Area}", property.Id.ToString(), property.Id == selected))
            .Prepend(new SelectListItem("Chọn căn hộ", string.Empty, selected == Guid.Empty))
            .ToArray();
    }

    private void PrepareView(string title)
    {
        ViewData["Title"] = title;
        ViewData["Breadcrumbs"] = new List<BreadcrumbItemViewModel>
        {
            new("Quản trị", "/admin/dashboard"),
            new("Hợp đồng chủ nhà", "/admin/landlord-contracts")
        };
    }

    private DateOnly Today()
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, BusinessTimeZone()).DateTime);

    private static TimeZoneInfo BusinessTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private bool CanDelete()
        => User.IsInRole(ApplicationRoles.Owner);

    private bool CanManage()
        => User.IsInRole(ApplicationRoles.Owner);

    private void AddErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }
}

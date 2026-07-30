using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Contracts;
using RealEstateManagement.Application.Properties;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Web.Areas.Admin.ViewModels;
using RealEstateManagement.Web.ViewModels.Shared;

namespace RealEstateManagement.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/tenant-contracts")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminOrSale)]
public sealed class TenantContractsController(
    ITenantContractService tenantContractService,
    IPropertyService propertyService,
    ISystemClock clock) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] ContractFilterViewModel filter, CancellationToken cancellationToken)
    {
        PrepareView("Hợp đồng khách thuê");
        var today = Today();
        var contracts = await tenantContractService.ListTenantContractsAsync(filter.ToQuery(today), cancellationToken);
        if (filter.SortBy == ContractSortOptions.SignedDate)
        {
            contracts = contracts.OrderBy(contract => contract.SignedDate).ThenBy(contract => contract.PropertyCode).ToArray();
        }

        var model = new TenantContractListViewModel
        {
            Filter = filter,
            Contracts = contracts.Select(contract => new TenantContractListItemViewModel(
                contract.Id,
                contract.PropertyId,
                contract.PropertyCode,
                PropertyDisplay.ProjectLabel(contract.Project),
                contract.Area,
                contract.TenantName,
                contract.ManagerName,
                contract.RentalPrice,
                contract.DepositAmount,
                contract.SignedDate,
                contract.ExpiryDate,
                contract.TermMonths,
                contract.Status,
                contract.PeCode,
                ContractDisplay.TenantWarnings(contract, today))).ToArray(),
            ProjectOptions = PropertyDisplay.ProjectOptions(filter.Project),
            StatusOptions = ContractDisplay.ContractStatusOptions(filter.Status),
            CanManage = CanManage(),
            CanDelete = CanDelete()
        };

        return View(model);
    }

    [HttpGet("create")]
    [Authorize(Policy = AuthorizationPolicies.CanManageContracts)]
    public async Task<IActionResult> Create(Guid? propertyId, CancellationToken cancellationToken)
    {
        PrepareView("Thêm hợp đồng khách thuê");
        return View(await PrepareFormAsync(new TenantContractFormViewModel { PropertyId = propertyId ?? Guid.Empty }, cancellationToken));
    }

    [HttpPost("create")]
    [Authorize(Policy = AuthorizationPolicies.CanManageContracts)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TenantContractFormViewModel model, CancellationToken cancellationToken)
    {
        PrepareView("Thêm hợp đồng khách thuê");
        if (!ModelState.IsValid)
        {
            return View(await PrepareFormAsync(model, cancellationToken));
        }

        var result = await tenantContractService.CreateTenantContractAsync(model.ToCommand(), cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(await PrepareFormAsync(model, cancellationToken));
        }

        TempData["SuccessMessage"] = "Đã thêm hợp đồng khách thuê.";
        return RedirectToAction(nameof(Details), new { id = result.ContractId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var contract = await tenantContractService.GetTenantContractAsync(id, cancellationToken);
        if (contract is null)
        {
            return NotFound();
        }

        PrepareView($"Hợp đồng khách thuê {contract.PropertyCode}");
        return View(new TenantContractDetailViewModel(contract, ContractDisplay.TenantWarnings(contract, Today()), CanManage(), CanDelete()));
    }

    [HttpGet("{id:guid}/edit")]
    [Authorize(Policy = AuthorizationPolicies.CanManageContracts)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var contract = await tenantContractService.GetTenantContractAsync(id, cancellationToken);
        if (contract is null)
        {
            return NotFound();
        }

        PrepareView($"Cập nhật hợp đồng {contract.PropertyCode}");
        return View(await PrepareFormAsync(TenantContractFormViewModel.FromDto(contract), cancellationToken));
    }

    [HttpPost("{id:guid}/edit")]
    [Authorize(Policy = AuthorizationPolicies.CanManageContracts)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TenantContractFormViewModel model, CancellationToken cancellationToken)
    {
        PrepareView("Cập nhật hợp đồng khách thuê");
        if (!ModelState.IsValid)
        {
            model.Id = id;
            return View(await PrepareFormAsync(model, cancellationToken));
        }

        var result = await tenantContractService.UpdateTenantContractAsync(id, model.ToCommand(), cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            model.Id = id;
            return View(await PrepareFormAsync(model, cancellationToken));
        }

        TempData["SuccessMessage"] = "Đã cập nhật hợp đồng khách thuê.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.CanManageContracts)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(Guid id, ContractStatus status, CancellationToken cancellationToken)
    {
        var result = await tenantContractService.ChangeStatusAsync(new TenantContractStatusCommand(id, status), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Đã cập nhật trạng thái hợp đồng."
            : string.Join(" ", result.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/delete")]
    [Authorize(Policy = AuthorizationPolicies.CanManageContracts)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await tenantContractService.DeleteTenantContractAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Đã xóa hợp đồng khách thuê."
            : string.Join(" ", result.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<TenantContractFormViewModel> PrepareFormAsync(TenantContractFormViewModel model, CancellationToken cancellationToken)
    {
        model.PropertyOptions = await PropertyOptionsAsync(model.PropertyId, cancellationToken);
        model.TermOptions = ContractDisplay.TermOptions(model.TermMonths);
        model.StatusOptions = ContractDisplay.ContractStatusOptions(model.Status);
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
            new("Hợp đồng khách thuê", "/admin/tenant-contracts")
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

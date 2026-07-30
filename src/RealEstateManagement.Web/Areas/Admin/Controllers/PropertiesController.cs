using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Contracts;
using RealEstateManagement.Application.Leads;
using RealEstateManagement.Application.Properties;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Web.Areas.Admin.ViewModels;
using RealEstateManagement.Web.ViewModels.Shared;

namespace RealEstateManagement.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/properties")]
[Authorize(Policy = "InternalUser")]
public sealed class PropertiesController(
    IPropertyService propertyService,
    ILandlordContractService landlordContractService,
    ITenantContractService tenantContractService,
    ILeadService leadService,
    ISystemClock clock) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] PropertyFilterViewModel filter, CancellationToken cancellationToken)
    {
        PrepareListView();

        if (filter.SortBy is not PropertySortOptions.Newest and not PropertySortOptions.Code)
        {
            ModelState.AddModelError(nameof(filter.SortBy), "Kiểu sắp xếp không hợp lệ.");
            filter.SortBy = PropertySortOptions.Newest;
        }

        var properties = await propertyService.ListPropertiesAsync(filter.ToQuery(), cancellationToken);
        properties = filter.SortBy == PropertySortOptions.Code
            ? properties.OrderBy(property => property.Code).ToArray()
            : properties.OrderByDescending(property => property.CreatedAtUtc).ThenBy(property => property.Code).ToArray();

        var model = new PropertyListViewModel
        {
            Filter = filter,
            Properties = properties.Select(ToListItem).ToArray(),
            CanDelete = CanDeleteProperties(),
            ProjectOptions = PropertyDisplay.ProjectOptions(filter.Project),
            TypeOptions = PropertyDisplay.TypeOptions(filter.Type),
            StatusOptions = PropertyDisplay.StatusOptions(filter.Status)
        };

        return View(model);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        PrepareFormView("Thêm căn hộ mới");
        return View(PrepareForm(PropertyFormViewModel.CreateDefault()));
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyFormViewModel model, CancellationToken cancellationToken)
    {
        PrepareFormView("Thêm căn hộ mới");
        if (!ModelState.IsValid)
        {
            return View(PrepareForm(model));
        }

        var result = await propertyService.CreatePropertyAsync(model.ToCommand(), cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(PrepareForm(model));
        }

        TempData["SuccessMessage"] = "Đã thêm căn hộ mới.";
        return RedirectToAction(nameof(Details), new { id = result.PropertyId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var property = await propertyService.GetPropertyDetailAsync(id, cancellationToken);
        if (property is null)
        {
            return NotFound();
        }

        ViewData["Title"] = property.Code;
        ViewData["Breadcrumbs"] = Breadcrumbs(property.Code);

        var landlordContract = await landlordContractService.GetLandlordContractForPropertyAsync(id, cancellationToken);
        var tenantContracts = await tenantContractService.ListTenantContractsForPropertyAsync(id, cancellationToken);
        var leads = await leadService.ListLeadsAsync(new LeadFilterQuery(PropertyId: id), cancellationToken);
        var today = Today();
        var activeTenantContract = tenantContracts
            .Where(contract => contract.Status == ContractStatus.Active && contract.ExpiryDate > today)
            .OrderBy(contract => contract.ExpiryDate)
            .FirstOrDefault();
        var monthlyMargin = landlordContract is not null && activeTenantContract is not null
            ? activeTenantContract.RentalPrice - landlordContract.InputPrice
            : (long?)null;

        return View(new PropertyDetailViewModel
        {
            Property = property,
            Contracts = new PropertyContractSummaryViewModel(
                landlordContract,
                activeTenantContract,
                tenantContracts,
                monthlyMargin,
                monthlyMargin * 12,
                ContractDisplay.PropertyWarnings(property.Status, landlordContract, activeTenantContract, monthlyMargin, today)),
            Leads = new PropertyLeadSummaryViewModel(
                leads.Count,
                leads.Count(lead => lead.Status == Domain.Leads.LeadStatus.New),
                leads.OrderByDescending(lead => lead.CreatedAtUtc).Take(5).ToArray()),
            CanDelete = CanDeleteProperties()
        });
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var property = await propertyService.GetPropertyDetailAsync(id, cancellationToken);
        if (property is null)
        {
            return NotFound();
        }

        PrepareFormView($"Cập nhật {property.Code}");
        return View(PrepareForm(PropertyFormViewModel.FromDetail(property)));
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PropertyFormViewModel model, CancellationToken cancellationToken)
    {
        PrepareFormView($"Cập nhật {model.Code}");
        if (!ModelState.IsValid)
        {
            model.Id = id;
            return View(PrepareForm(model));
        }

        var result = await propertyService.UpdatePropertyAsync(id, model.ToCommand(), cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            model.Id = id;
            return View(PrepareForm(model));
        }

        TempData["SuccessMessage"] = "Đã cập nhật căn hộ.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/delete")]
    [Authorize(Policy = "OwnerOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await propertyService.DeletePropertyAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = "Đã xóa căn hộ.";
        return RedirectToAction(nameof(Index));
    }

    private static PropertyListItemViewModel ToListItem(PropertySummaryDto property)
        => new(
            property.Id,
            property.Code,
            PropertyDisplay.ProjectLabel(property.Project),
            property.Area,
            PropertyDisplay.TypeLabel(property.Type),
            property.AreaSize,
            property.MonthlyPrice,
            property.SalePrice,
            PropertyDisplay.StatusLabel(property.Status),
            PropertyDisplay.StatusTone(property.Status),
            property.AvailableFromDate);

    private PropertyFormViewModel PrepareForm(PropertyFormViewModel model)
    {
        model.ProjectOptions = PropertyDisplay.ProjectOptions(model.Project);
        model.TypeOptions = PropertyDisplay.TypeOptions(model.Type);
        model.StatusOptions = PropertyDisplay.StatusOptions(model.Status);
        return model;
    }

    private void PrepareListView()
    {
        ViewData["Title"] = "Căn hộ";
        ViewData["Breadcrumbs"] = Breadcrumbs();
    }

    private void PrepareFormView(string title)
    {
        ViewData["Title"] = title;
        ViewData["Breadcrumbs"] = Breadcrumbs(title);
    }

    private static List<BreadcrumbItemViewModel> Breadcrumbs(string? current = null)
    {
        var breadcrumbs = new List<BreadcrumbItemViewModel>
        {
            new("Quản trị", "/admin/dashboard"),
            new("Căn hộ", "/admin/properties")
        };

        if (!string.IsNullOrWhiteSpace(current))
        {
            breadcrumbs.Add(new(current));
        }

        return breadcrumbs;
    }

    private bool CanDeleteProperties()
        => User.IsInRole(ApplicationRoles.Owner);

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

    private void AddErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }
}

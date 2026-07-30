using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Application.Leads;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Infrastructure.Identity;
using RealEstateManagement.Web.Areas.Admin.ViewModels;
using RealEstateManagement.Web.ViewModels.Shared;

namespace RealEstateManagement.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/leads")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminOrSale)]
public sealed class LeadsController(
    ILeadService leadService,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] LeadFilterViewModel filter, CancellationToken cancellationToken)
    {
        PrepareView("Lead");
        var query = ApplyAccessScope(filter.ToQuery());
        var leads = await leadService.ListLeadsAsync(query, cancellationToken);

        var model = new LeadListViewModel
        {
            Filter = filter,
            Leads = leads.Select(ToListItem).ToArray(),
            CanAssign = CanManageAllLeads(),
            StatusOptions = LeadDisplay.StatusOptions(filter.Status),
            AssignedUserOptions = await AssignedUserOptionsAsync(filter.AssignedToUserId),
            ProjectOptions = PropertyDisplay.ProjectOptions(filter.Project)
        };

        return View(model);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var lead = await leadService.GetLeadAsync(id, cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        if (!CanAccessLead(lead))
        {
            return Forbid();
        }

        PrepareView(lead.Name);
        var model = new LeadDetailViewModel
        {
            Lead = lead,
            StatusForm = new LeadStatusFormViewModel { Status = lead.Status },
            AssignmentForm = new LeadAssignmentFormViewModel { SaleUserId = lead.AssignedToUserId ?? Guid.Empty },
            Property = ToPropertySummary(lead),
            CanAssign = CanManageAllLeads(),
            CanUpdateStatus = CanAccessLead(lead),
            StatusOptions = LeadDisplay.StatusOptions(lead.Status),
            AssignedUserOptions = await AssignedUserOptionsAsync(lead.AssignedToUserId)
        };

        return View(model);
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.CanUpdateAssignedLead)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(Guid id, LeadStatusFormViewModel model, CancellationToken cancellationToken)
    {
        var lead = await leadService.GetLeadAsync(id, cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        if (!CanAccessLead(lead))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn trạng thái hợp lệ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await leadService.ChangeStatusAsync(new LeadStatusCommand(
            id,
            model.Status,
            CurrentUserId(),
            CanManageAllLeads()), cancellationToken);

        SetResultMessage(result, "Đã cập nhật trạng thái lead.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = AuthorizationPolicies.CanAssignLeads)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid id, LeadAssignmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn Sale phụ trách.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await leadService.AssignLeadAsync(new LeadAssignmentCommand(
            id,
            model.SaleUserId,
            CurrentUserId(),
            ActorCanManageAll: true), cancellationToken);

        SetResultMessage(result, "Đã phân công lead.");
        return RedirectToAction(nameof(Details), new { id });
    }

    private LeadFilterQuery ApplyAccessScope(LeadFilterQuery query)
    {
        if (CanManageAllLeads())
        {
            return query;
        }

        var userId = CurrentUserId();
        return query with { AssignedToUserId = userId, UnassignedOnly = false };
    }

    private bool CanAccessLead(LeadDto lead)
        => CanManageAllLeads() || lead.AssignedToUserId == CurrentUserId();

    private bool CanManageAllLeads()
        => User.IsInRole(ApplicationRoles.Owner);

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
    }

    private static LeadListItemViewModel ToListItem(LeadDto lead)
        => new(
            lead.Id,
            lead.Name,
            lead.Contact,
            lead.PropertyCode,
            lead.PropertyArea,
            lead.Subject,
            LeadDisplay.LanguageLabel(lead.Language),
            LeadDisplay.StatusLabel(lead.Status),
            LeadDisplay.StatusTone(lead.Status),
            lead.AssignedToDisplayName,
            lead.CreatedAtUtc,
            lead.UpdatedAtUtc);

    private static LeadPropertySummaryViewModel? ToPropertySummary(LeadDto lead)
    {
        if (lead.PropertyId is null || lead.PropertyCode is null || lead.PropertyType is null)
        {
            return null;
        }

        return new LeadPropertySummaryViewModel(
            lead.PropertyId.Value,
            lead.PropertyCode,
            lead.PropertyArea,
            PropertyDisplay.TypeLabel(lead.PropertyType.Value),
            PropertyDisplay.FormatMoney(lead.PropertyMonthlyPrice),
            PropertyDisplay.FormatMoney(lead.PropertySalePrice),
            lead.PropertyStatus is null ? "Chưa có" : PropertyDisplay.StatusLabel(lead.PropertyStatus.Value));
    }

    private async Task<IReadOnlyList<SelectListItem>> AssignedUserOptionsAsync(Guid? selected)
    {
        var users = new Dictionary<Guid, ApplicationUser>();
        foreach (var user in await userManager.GetUsersInRoleAsync(ApplicationRoles.Staff))
        {
            users[user.Id] = user;
        }

        foreach (var user in await userManager.GetUsersInRoleAsync(ApplicationRoles.Owner))
        {
            users[user.Id] = user;
        }

        return users.Values
            .OrderBy(user => user.DisplayName ?? user.FullName)
            .Select(user => new SelectListItem(user.DisplayName ?? user.FullName, user.Id.ToString(), user.Id == selected))
            .Prepend(new SelectListItem("Chưa phân công", string.Empty, selected is null || selected == Guid.Empty))
            .ToArray();
    }

    private void PrepareView(string current)
    {
        ViewData["Title"] = current;
        ViewData["Breadcrumbs"] = new List<BreadcrumbItemViewModel>
        {
            new("Quản trị", "/admin/dashboard"),
            new("Lead", "/admin/leads"),
            new(current)
        };
    }

    private void SetResultMessage(LeadCommandResult result, string successMessage)
    {
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = successMessage;
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }
    }
}

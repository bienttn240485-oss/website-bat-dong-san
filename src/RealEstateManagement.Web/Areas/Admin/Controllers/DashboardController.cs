using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Application.Dashboard;
using RealEstateManagement.Web.Areas.Admin.ViewModels;

namespace RealEstateManagement.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/dashboard")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminOrSale)]
public sealed class DashboardController(IDashboardService dashboardService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var canViewFinancials = User.IsInRole(ApplicationRoles.Owner);
        var snapshot = await dashboardService.GetDashboardAsync(ResolveScope(canViewFinancials), cancellationToken);
        return View(AdminDashboardViewModel.FromSnapshot(snapshot, canViewFinancials));
    }

    [HttpGet("/admin/api/dashboard/gmv")]
    [Authorize(Policy = AuthorizationPolicies.CanViewFinancialDashboard)]
    public async Task<IActionResult> Gmv(CancellationToken cancellationToken)
        => Json((await dashboardService.GetDashboardAsync(cancellationToken)).Charts.GmvLast12Months);

    [HttpGet("/admin/api/dashboard/contracts")]
    public async Task<IActionResult> Contracts(CancellationToken cancellationToken)
        => Json((await dashboardService.GetDashboardAsync(ResolveScope(User.IsInRole(ApplicationRoles.Owner)), cancellationToken)).Charts.ContractsSignedLast12Months);

    [HttpGet("/admin/api/dashboard/property-status")]
    public async Task<IActionResult> PropertyStatus(CancellationToken cancellationToken)
        => Json((await dashboardService.GetDashboardAsync(ResolveScope(User.IsInRole(ApplicationRoles.Owner)), cancellationToken)).Charts.PropertyStatusDistribution);

    [HttpGet("/admin/api/dashboard/lead-status")]
    public async Task<IActionResult> LeadStatus(CancellationToken cancellationToken)
        => Json((await dashboardService.GetDashboardAsync(ResolveScope(User.IsInRole(ApplicationRoles.Owner)), cancellationToken)).Charts.LeadStatusDistribution);

    private DashboardScope ResolveScope(bool canViewAllData)
        => canViewAllData
            ? new DashboardScope()
            : Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? new DashboardScope(userId)
                : new DashboardScope(Guid.Empty);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Application.Dashboard;
using RealEstateManagement.Application.Reports;
using RealEstateManagement.Web.Areas.Admin.ViewModels;

namespace RealEstateManagement.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/dashboard")]
[Authorize(Policy = "InternalUser")]
public sealed class DashboardController(IDashboardService dashboardService, IReportService reportService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var snapshot = await dashboardService.GetDashboardAsync(cancellationToken);
        var canViewFinancials = User.IsInRole(ApplicationRoles.Owner);
        return View(AdminDashboardViewModel.FromSnapshot(snapshot, canViewFinancials));
    }

    [HttpGet("/admin/api/dashboard/gmv")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Gmv(CancellationToken cancellationToken)
        => Json((await dashboardService.GetDashboardAsync(cancellationToken)).Charts.GmvLast12Months);

    [HttpGet("/admin/api/dashboard/contracts")]
    public async Task<IActionResult> Contracts(CancellationToken cancellationToken)
        => Json((await dashboardService.GetDashboardAsync(cancellationToken)).Charts.ContractsSignedLast12Months);

    [HttpGet("/admin/api/dashboard/property-status")]
    public async Task<IActionResult> PropertyStatus(CancellationToken cancellationToken)
        => Json((await dashboardService.GetDashboardAsync(cancellationToken)).Charts.PropertyStatusDistribution);

    [HttpGet("/admin/api/dashboard/lead-status")]
    public async Task<IActionResult> LeadStatus(CancellationToken cancellationToken)
        => Json((await dashboardService.GetDashboardAsync(cancellationToken)).Charts.LeadStatusDistribution);

    [HttpGet("/admin/api/dashboard/revenue")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Revenue([FromQuery] string? from, [FromQuery] string? to, CancellationToken cancellationToken)
    {
        var (fromDate, toDate) = ResolveRange(from, to);
        return Json(await reportService.GetRevenueChartAsync(fromDate, toDate, cancellationToken));
    }

    [HttpGet("/admin/api/dashboard/bookings")]
    public async Task<IActionResult> Bookings([FromQuery] string? from, [FromQuery] string? to, CancellationToken cancellationToken)
    {
        var (fromDate, toDate) = ResolveRange(from, to);
        return Json(await reportService.GetBookingCountChartAsync(fromDate, toDate, cancellationToken));
    }

    [HttpGet("/admin/api/dashboard/utilization")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Utilization([FromQuery] string? from, [FromQuery] string? to, CancellationToken cancellationToken)
    {
        var (fromDate, toDate) = ResolveRange(from, to);
        return Json(await reportService.GetUtilizationChartAsync(fromDate, toDate, cancellationToken));
    }

    private static (DateOnly FromDate, DateOnly ToDate) ResolveRange(string? from, string? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var weekStart = today.AddDays(-6);
        var fromDate = DateOnly.TryParse(from, out var parsedFrom) ? parsedFrom : weekStart;
        var toDate = DateOnly.TryParse(to, out var parsedTo) ? parsedTo : today;
        return fromDate <= toDate ? (fromDate, toDate) : (toDate, fromDate);
    }
}

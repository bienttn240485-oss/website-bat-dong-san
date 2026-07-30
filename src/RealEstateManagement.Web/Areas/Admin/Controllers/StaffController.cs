using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Domain.Users;
using RealEstateManagement.Infrastructure.Identity;
using RealEstateManagement.Web.Areas.Admin.ViewModels;
using RealEstateManagement.Web.ViewModels.Shared;

namespace RealEstateManagement.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/staff")]
[Authorize(Policy = AuthorizationPolicies.CanManageStaff)]
public sealed class StaffController(UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Nhân viên";
        ViewData["Breadcrumbs"] = Breadcrumbs("Nhân viên");

        var staffUsers = await userManager.GetUsersInRoleAsync(ApplicationRoles.Staff);
        var model = staffUsers
            .OrderBy(user => user.FullName)
            .Select(user => new StaffListItemViewModel(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                user.AccountStatus,
                user.CreatedAtUtc,
                user.LastLoginAtUtc))
            .ToList();

        cancellationToken.ThrowIfCancellationRequested();
        return View("~/Areas/Admin/Views/Shared/StaffIndex.cshtml", model);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        PrepareForm("Thêm nhân viên");
        return View("~/Areas/Admin/Views/Shared/StaffForm.cshtml", new StaffFormViewModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffFormViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Vui lòng nhập mật khẩu tạm.");
        }

        if (!ModelState.IsValid)
        {
            PrepareForm("Thêm nhân viên");
            return View("~/Areas/Admin/Views/Shared/StaffForm.cshtml", model);
        }

        var email = model.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
            PrepareForm("Thêm nhân viên");
            return View("~/Areas/Admin/Views/Shared/StaffForm.cshtml", model);
        }

        var now = DateTimeOffset.UtcNow;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            AccountStatus = model.IsActive ? AccountStatus.Active : AccountStatus.Locked,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, model.Password!);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            PrepareForm("Thêm nhân viên");
            return View("~/Areas/Admin/Views/Shared/StaffForm.cshtml", model);
        }

        var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Staff);
        if (!roleResult.Succeeded)
        {
            AddIdentityErrors(roleResult);
            PrepareForm("Thêm nhân viên");
            return View("~/Areas/Admin/Views/Shared/StaffForm.cshtml", model);
        }

        if (!model.IsActive)
        {
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }

        cancellationToken.ThrowIfCancellationRequested();
        TempData["SuccessMessage"] = "Đã thêm tài khoản nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await FindStaffAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        PrepareForm("Cập nhật nhân viên");
        return View("~/Areas/Admin/Views/Shared/StaffForm.cshtml", new StaffFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.AccountStatus == AccountStatus.Active
        });
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, StaffFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        var user = await FindStaffAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            PrepareForm("Cập nhật nhân viên");
            return View("~/Areas/Admin/Views/Shared/StaffForm.cshtml", model);
        }

        var email = model.Email.Trim();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null && existing.Id != id)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
            PrepareForm("Cập nhật nhân viên");
            return View("~/Areas/Admin/Views/Shared/StaffForm.cshtml", model);
        }

        user.FullName = model.FullName.Trim();
        user.Email = email;
        user.UserName = email;
        user.AccountStatus = model.IsActive ? AccountStatus.Active : AccountStatus.Locked;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            AddIdentityErrors(updateResult);
            PrepareForm("Cập nhật nhân viên");
            return View("~/Areas/Admin/Views/Shared/StaffForm.cshtml", model);
        }

        await userManager.SetLockoutEndDateAsync(user, model.IsActive ? null : DateTimeOffset.MaxValue);

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await userManager.ResetPasswordAsync(user, token, model.Password);
            if (!passwordResult.Succeeded)
            {
                AddIdentityErrors(passwordResult);
                PrepareForm("Cập nhật nhân viên");
                return View("~/Areas/Admin/Views/Shared/StaffForm.cshtml", model);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        TempData["SuccessMessage"] = "Đã cập nhật tài khoản nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var user = await FindStaffAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.AccountStatus = isActive ? AccountStatus.Active : AccountStatus.Locked;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);
        await userManager.SetLockoutEndDateAsync(user, isActive ? null : DateTimeOffset.MaxValue);

        cancellationToken.ThrowIfCancellationRequested();
        TempData["SuccessMessage"] = isActive ? "Đã mở khóa tài khoản nhân viên." : "Đã khóa tài khoản nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ApplicationUser?> FindStaffAsync(Guid id)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(item => item.Id == id);
        return user is not null && await userManager.IsInRoleAsync(user, ApplicationRoles.Staff) ? user : null;
    }

    private void PrepareForm(string title)
    {
        ViewData["Title"] = title;
        ViewData["Breadcrumbs"] = Breadcrumbs(title);
    }

    private static List<BreadcrumbItemViewModel> Breadcrumbs(string current)
        =>
        [
            new("Quản trị", "/admin/dashboard"),
            new("Nhân viên", "/admin/staff"),
            new(current)
        ];

    private void AddIdentityErrors(IdentityResult result)
    {
        var hasPasswordError = result.Errors.Any(error => error.Code.Contains("Password", StringComparison.OrdinalIgnoreCase));
        ModelState.AddModelError(
            string.Empty,
            hasPasswordError
                ? "Mật khẩu chưa đủ mạnh. Vui lòng dùng ít nhất 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt."
                : "Không thể lưu tài khoản nhân viên. Vui lòng kiểm tra lại thông tin.");
    }
}

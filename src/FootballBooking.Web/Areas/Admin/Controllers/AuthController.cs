using FootballBooking.Application.Common.Security;
using FootballBooking.Infrastructure.Identity;
using FootballBooking.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FootballBooking.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin")]
public sealed class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true
            && (User.IsInRole(ApplicationRoles.Owner) || User.IsInRole(ApplicationRoles.Staff)))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new AdminLoginViewModel());
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        var isInternalUser = await userManager.IsInRoleAsync(user, ApplicationRoles.Owner)
            || await userManager.IsInRoleAsync(user, ApplicationRoles.Staff);
        if (!isInternalUser)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản này không có quyền truy cập khu vực quản trị.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        user.LastLoginAtUtc = DateTimeOffset.UtcNow;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    [HttpPost("logout")]
    [Authorize(Policy = "InternalUser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}

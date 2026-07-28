using RealEstateManagement.Application.Common.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RealEstateManagement.Infrastructure.Identity;

public sealed class IdentitySeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<DevelopmentOwnerOptions> ownerOptions,
    IOptions<DevelopmentInternalUsersOptions> internalUsersOptions,
    ILogger<IdentitySeeder> logger)
{
    public async Task SeedRolesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                ThrowIfFailed(result, $"Không thể tạo vai trò {roleName}.");
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public async Task SeedDevelopmentOwnerAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(cancellationToken);

        if (internalUsersOptions.Value.Users.Count > 0)
        {
            await SeedDevelopmentInternalUsersAsync(cancellationToken);
            return;
        }

        var options = ownerOptions.Value;
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning("Bá» qua seed tÃ i khoản Owner vì SeedOwner:Email hoặc SeedOwner:Password chưa được cấu hình.");
            return;
        }

        var email = options.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = string.IsNullOrWhiteSpace(options.FullName) ? "Chủ sân" : options.FullName.Trim(),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, options.Password);
            ThrowIfFailed(createResult, "Không thể tạo tÃ i khoản Owner development.");
        }

        if (!await userManager.IsInRoleAsync(user, ApplicationRoles.Owner))
        {
            var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Owner);
            ThrowIfFailed(roleResult, "Không thể gán vai trò Owner cho tÃ i khoản development.");
        }
    }

    private async Task SeedDevelopmentInternalUsersAsync(CancellationToken cancellationToken)
    {
        foreach (var options in internalUsersOptions.Value.Users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(options.Email)
                || string.IsNullOrWhiteSpace(options.Password)
                || string.IsNullOrWhiteSpace(options.Role))
            {
                logger.LogWarning("Bá» qua seed tÃ i khoản nội bộ vì thiếu email, mật khẩu hoặc vai trò.");
                continue;
            }

            var role = options.Role.Trim();
            if (role != ApplicationRoles.Owner && role != ApplicationRoles.Staff)
            {
                logger.LogWarning("Bá» qua seed tÃ i khoản {Email} vì vai trò {Role} không hợp lệ.", options.Email, role);
                continue;
            }

            var email = options.Email.Trim();
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = string.IsNullOrWhiteSpace(options.FullName) ? email : options.FullName.Trim(),
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                };

                var createResult = await userManager.CreateAsync(user, options.Password);
                ThrowIfFailed(createResult, $"Không thể tạo tÃ i khoản {email}.");
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);
                ThrowIfFailed(roleResult, $"Không thể gán vai trò {role} cho tÃ i khoản {email}.");
            }
        }
    }

    private static void ThrowIfFailed(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"{message} {errors}");
    }
}


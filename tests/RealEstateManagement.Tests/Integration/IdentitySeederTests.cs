using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Infrastructure.Data;
using RealEstateManagement.Infrastructure.Identity;

namespace RealEstateManagement.Tests.Integration;

public sealed class IdentitySeederTests
{
    [Fact]
    public async Task SeedDevelopmentOwnerAsync_WhenConfigured_CreatesExactlyOneAdminAndFiveSaleUsers()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.AddSingleton(Options.Create(new DevelopmentOwnerOptions
        {
            Email = "admin@anphurealestate.local",
            Password = "LocalOnly!12345",
            FullName = "Quản trị An Phú"
        }));
        services.AddSingleton(Options.Create(new DevelopmentInternalUsersOptions
        {
            Users =
            [
                new() { Email = "sale.tham@anphurealestate.local", Password = "LocalOnly!12345", FullName = "Nguyễn Thị Thắm", Role = ApplicationRoles.Sale },
                new() { Email = "sale.thuy@anphurealestate.local", Password = "LocalOnly!12345", FullName = "Trần Thu Thủy", Role = ApplicationRoles.Sale },
                new() { Email = "sale.tuan@anphurealestate.local", Password = "LocalOnly!12345", FullName = "Nguyễn Minh Tuấn", Role = ApplicationRoles.Sale },
                new() { Email = "sale.linh@anphurealestate.local", Password = "LocalOnly!12345", FullName = "Lê Hoài Linh", Role = ApplicationRoles.Sale },
                new() { Email = "sale.huy@anphurealestate.local", Password = "LocalOnly!12345", FullName = "Phạm Quốc Huy", Role = ApplicationRoles.Sale }
            ]
        }));
        services.AddScoped<IdentitySeeder>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedDevelopmentOwnerAsync();
        await seeder.SeedDevelopmentOwnerAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.True(await roleManager.RoleExistsAsync(ApplicationRoles.Admin));
        Assert.True(await roleManager.RoleExistsAsync(ApplicationRoles.Sale));
        Assert.False(await roleManager.RoleExistsAsync("Customer"));
        Assert.False(await roleManager.RoleExistsAsync("Owner"));
        Assert.False(await roleManager.RoleExistsAsync("Staff"));

        var admin = await userManager.FindByEmailAsync("admin@anphurealestate.local");
        Assert.NotNull(admin);
        Assert.True(await userManager.IsInRoleAsync(admin, ApplicationRoles.Admin));

        var adminRoleId = await dbContext.Roles.Where(role => role.Name == ApplicationRoles.Admin).Select(role => role.Id).SingleAsync();
        var saleRoleId = await dbContext.Roles.Where(role => role.Name == ApplicationRoles.Sale).Select(role => role.Id).SingleAsync();
        Assert.Equal(1, await dbContext.UserRoles.CountAsync(userRole => userRole.RoleId == adminRoleId));
        Assert.Equal(5, await dbContext.UserRoles.CountAsync(userRole => userRole.RoleId == saleRoleId));
        Assert.Equal(6, await dbContext.Users.CountAsync());
    }
}

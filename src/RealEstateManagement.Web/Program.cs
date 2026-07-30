using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Infrastructure;
using RealEstateManagement.Infrastructure.Identity;
using RealEstateManagement.Infrastructure.SeedData;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;

var builder = WebApplication.CreateBuilder(args);
var shouldSeedDevelopmentData = args.Any(arg => arg.Equals("--seed-development-data", StringComparison.OrdinalIgnoreCase));

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.RequireAdmin, policy => policy.RequireRole(ApplicationRoles.Owner));
    options.AddPolicy(AuthorizationPolicies.RequireAdminOrSale, policy => policy.RequireRole(ApplicationRoles.Internal));
    options.AddPolicy(AuthorizationPolicies.CanManageProperties, policy => policy.RequireRole(ApplicationRoles.Owner));
    options.AddPolicy(AuthorizationPolicies.CanManageContracts, policy => policy.RequireRole(ApplicationRoles.Owner));
    options.AddPolicy(AuthorizationPolicies.CanViewFinancialDashboard, policy => policy.RequireRole(ApplicationRoles.Owner));
    options.AddPolicy(AuthorizationPolicies.CanManageStaff, policy => policy.RequireRole(ApplicationRoles.Owner));
    options.AddPolicy(AuthorizationPolicies.CanAssignLeads, policy => policy.RequireRole(ApplicationRoles.Owner));
    options.AddPolicy(AuthorizationPolicies.CanUpdateAssignedLead, policy => policy.RequireRole(ApplicationRoles.Internal));
    options.AddPolicy(AuthorizationPolicies.LegacyInternalUser, policy => policy.RequireRole(ApplicationRoles.Internal));
    options.AddPolicy(AuthorizationPolicies.LegacyOwnerOnly, policy => policy.RequireRole(ApplicationRoles.Owner));
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/login";
    options.AccessDeniedPath = "/admin/login";
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

var app = builder.Build();

if (shouldSeedDevelopmentData)
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Chỉ được seed dữ liệu mẫu khi môi trường là Development.");
    }

    using var seedScope = app.Services.CreateScope();
    var identitySeeder = seedScope.ServiceProvider.GetRequiredService<IdentitySeeder>();
    await identitySeeder.SeedDevelopmentOwnerAsync();
    await seedScope.ServiceProvider.GetRequiredService<DevelopmentRealEstateSeeder>().SeedAsync();
    // await seedScope.ServiceProvider.GetRequiredService<DevelopmentFieldSeeder>().SeedAsync();
    // await seedScope.ServiceProvider.GetRequiredService<DevelopmentCommerceSeeder>().SeedAsync();
    // await seedScope.ServiceProvider.GetRequiredService<DevelopmentOperationsSeeder>().SeedAsync();
    return;
}

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var hasHttpsRedirectPort = !string.IsNullOrWhiteSpace(app.Configuration["HTTPS_PORT"])
    || !string.IsNullOrWhiteSpace(app.Configuration["ASPNETCORE_HTTPS_PORTS"])
    || !string.IsNullOrWhiteSpace(app.Configuration["HttpsRedirection:HttpsPort"]);
if (!app.Environment.IsDevelopment() && hasHttpsRedirectPort)
{
    app.UseHttpsRedirection();
}
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

public partial class Program;

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Infrastructure.Data;
using RealEstateManagement.Infrastructure.Identity;
using RealEstateManagement.Infrastructure.SeedData;

namespace RealEstateManagement.Tests.Integration;

public sealed class AdminAuthorizationMatrixTests
{
    [Fact]
    public async Task AdminRoutes_WhenAnonymous_AreChallenged()
    {
        await using var factory = await AuthorizationFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/admin/dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Dashboard_WhenOwnerAndStaffAuthenticated_AppliesFinancialPolicy()
    {
        await using var factory = await AuthorizationFactory.CreateAsync();
        var staffId = await factory.FindUserIdAsync("staff@example.test");
        await factory.CreateLeadAsync("Lead Dashboard Assigned", "dashboard-assigned@example.test", staffId);
        await factory.CreateLeadAsync("Lead Dashboard Other", "dashboard-other@example.test", null);
        var assignedLeadCount = await factory.CountLeadsAssignedToAsync(staffId);
        using var owner = factory.CreateClient();
        await LoginAsync(owner, "owner@example.test", AuthorizationFactory.Password);
        using var staff = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(staff, "staff@example.test", AuthorizationFactory.Password);

        var ownerDashboard = await owner.GetAsync("/admin/dashboard");
        var ownerContent = await ReadDecodedContentAsync(ownerDashboard);
        var staffDashboard = await staff.GetAsync("/admin/dashboard");
        var staffContent = await ReadDecodedContentAsync(staffDashboard);
        var staffGmv = await staff.GetAsync("/admin/api/dashboard/gmv");

        Assert.Equal(HttpStatusCode.OK, ownerDashboard.StatusCode);
        Assert.Contains("Tổng giá vào/tháng", ownerContent);
        Assert.Equal(HttpStatusCode.OK, staffDashboard.StatusCode);
        Assert.Contains("Tài khoản hiện tại không có quyền xem số liệu tài chính nhạy cảm.", staffContent);
        Assert.DoesNotContain("Tổng giá vào/tháng", staffContent);
        Assert.Matches($"Tổng Lead[\\s\\S]{{0,500}}>{assignedLeadCount}<", staffContent);
        Assert.Matches("Lead chưa phân công[\\s\\S]{0,500}>0<", staffContent);
        Assert.Equal(HttpStatusCode.Forbidden, staffGmv.StatusCode);
    }

    [Fact]
    public async Task Staff_WhenManipulatingPropertyOrContractUrls_IsForbidden()
    {
        await using var factory = await AuthorizationFactory.CreateAsync();
        var propertyId = await factory.FindPropertyIdAsync("OP-0101");
        var landlordContractId = await factory.FindLandlordContractIdAsync(propertyId);
        var tenantContractId = await factory.FindTenantContractIdAsync(propertyId);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "staff@example.test", AuthorizationFactory.Password);

        var propertyCreate = await client.GetAsync("/admin/properties/create");
        var propertyEdit = await client.GetAsync($"/admin/properties/{propertyId}/edit");
        var landlordCreate = await client.GetAsync("/admin/landlord-contracts/create");
        var landlordEdit = await client.GetAsync($"/admin/landlord-contracts/{landlordContractId}/edit");
        var tenantCreate = await client.GetAsync("/admin/tenant-contracts/create");
        var tenantEdit = await client.GetAsync($"/admin/tenant-contracts/{tenantContractId}/edit");
        var token = await GetAntiforgeryTokenAsync(client, $"/admin/properties/{propertyId}");
        var deleteProperty = await client.PostAsync($"/admin/properties/{propertyId}/delete", Form([
            ("__RequestVerificationToken", token)
        ]));
        token = await GetAntiforgeryTokenAsync(client, $"/admin/tenant-contracts/{tenantContractId}");
        var deleteTenant = await client.PostAsync($"/admin/tenant-contracts/{tenantContractId}/delete", Form([
            ("__RequestVerificationToken", token)
        ]));
        token = await GetAntiforgeryTokenAsync(client, $"/admin/landlord-contracts/{landlordContractId}");
        var deleteLandlord = await client.PostAsync($"/admin/landlord-contracts/{landlordContractId}/delete", Form([
            ("__RequestVerificationToken", token)
        ]));

        Assert.Equal(HttpStatusCode.Forbidden, propertyCreate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, propertyEdit.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, landlordCreate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, landlordEdit.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, tenantCreate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, tenantEdit.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteProperty.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteTenant.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteLandlord.StatusCode);
    }

    [Fact]
    public async Task Leads_WhenStaffAuthenticated_AreScopedToAssignedLead()
    {
        await using var factory = await AuthorizationFactory.CreateAsync();
        var staffId = await factory.FindUserIdAsync("staff@example.test");
        var assignedLeadId = await factory.CreateLeadAsync("Lead Assigned", "assigned@example.test", staffId);
        var otherLeadId = await factory.CreateLeadAsync("Lead Other", "other@example.test", null);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "staff@example.test", AuthorizationFactory.Password);

        var list = await client.GetAsync("/admin/leads");
        var listContent = await ReadDecodedContentAsync(list);
        var assignedDetail = await client.GetAsync($"/admin/leads/{assignedLeadId}");
        var otherDetail = await client.GetAsync($"/admin/leads/{otherLeadId}");
        var token = await GetAntiforgeryTokenAsync(client, $"/admin/leads/{assignedLeadId}");
        var updateOther = await client.PostAsync($"/admin/leads/{otherLeadId}/status", Form([
            ("__RequestVerificationToken", token),
            ("Status", LeadStatus.Contacted.ToString())
        ]));
        var assignOther = await client.PostAsync($"/admin/leads/{assignedLeadId}/assign", Form([
            ("__RequestVerificationToken", token),
            ("SaleUserId", staffId.ToString())
        ]));

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains("Lead Assigned", listContent);
        Assert.DoesNotContain("Lead Other", listContent);
        Assert.Equal(HttpStatusCode.OK, assignedDetail.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherDetail.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateOther.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, assignOther.StatusCode);
    }

    [Fact]
    public async Task Staff_WhenAuthenticated_CannotAccessStaffOrReportsAndMenuOmitsSensitiveLinks()
    {
        await using var factory = await AuthorizationFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "staff@example.test", AuthorizationFactory.Password);

        var staffRoute = await client.GetAsync("/admin/staff");
        var reports = await client.GetAsync("/admin/reports");
        var dashboard = await client.GetAsync("/admin/dashboard");
        var content = await ReadDecodedContentAsync(dashboard);

        Assert.Equal(HttpStatusCode.Forbidden, staffRoute.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reports.StatusCode);
        Assert.Contains("Lead của tôi", content);
        Assert.DoesNotContain("Nhân sự", content);
        Assert.DoesNotContain("Hệ thống cũ:", content);
        Assert.DoesNotContain("/admin/reports", content);
    }

    [Fact]
    public async Task Owner_WhenAuthenticated_CanManageLeadAndStaff()
    {
        await using var factory = await AuthorizationFactory.CreateAsync();
        var leadId = await factory.CreateLeadAsync("Lead Admin Assign", "admin-assign@example.test", null);
        var staffId = await factory.FindUserIdAsync("staff@example.test");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "owner@example.test", AuthorizationFactory.Password);

        var staffRoute = await client.GetAsync("/admin/staff");
        var token = await GetAntiforgeryTokenAsync(client, $"/admin/leads/{leadId}");
        var assign = await client.PostAsync($"/admin/leads/{leadId}/assign", Form([
            ("__RequestVerificationToken", token),
            ("SaleUserId", staffId.ToString())
        ]));

        Assert.Equal(HttpStatusCode.OK, staffRoute.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, assign.StatusCode);
    }

    [Fact]
    public async Task AuthorizationViews_DoNotContainMojibakeInChangedFiles()
    {
        foreach (var path in new[]
        {
            Path.Combine(AuthorizationFactory.ContentRoot, "Areas", "Admin", "Views", "Shared", "Partials", "_AdminSidebar.cshtml"),
            Path.Combine(AuthorizationFactory.ContentRoot, "Areas", "Admin", "Views", "Dashboard", "Index.cshtml"),
            Path.Combine(AuthorizationFactory.ContentRoot, "Areas", "Admin", "Views", "Properties", "Details.cshtml"),
            Path.Combine(AuthorizationFactory.ContentRoot, "Areas", "Admin", "Views", "LandlordContracts", "Index.cshtml"),
            Path.Combine(AuthorizationFactory.ContentRoot, "Areas", "Admin", "Views", "TenantContracts", "Index.cshtml")
        })
        {
            AssertNoMojibake(await File.ReadAllTextAsync(path));
        }
    }

    private static FormUrlEncodedContent Form(IEnumerable<(string Key, string Value)> values)
        => new(values.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value)));

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/admin/login");
        var response = await client.PostAsync("/admin/login", Form([
            ("__RequestVerificationToken", token),
            ("Email", email),
            ("Password", password),
            ("RememberMe", "false")
        ]));

        Assert.True(
            response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect,
            $"Login failed with status {(int)response.StatusCode}.");
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var content = await ReadDecodedContentAsync(response);
        var match = Regex.Match(content, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"");
        Assert.True(match.Success, $"Antiforgery token not found on {path}.");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private static async Task<string> ReadDecodedContentAsync(HttpResponseMessage response)
        => WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

    private static void AssertNoMojibake(string content)
    {
        Assert.DoesNotContain(((char)0x00C3).ToString(), content);
        Assert.DoesNotContain(((char)0x00C4).ToString(), content);
        Assert.DoesNotContain($"{(char)0x00E1}{(char)0x00BA}", content);
        Assert.DoesNotContain($"{(char)0x00E1}{(char)0x00BB}", content);
        Assert.DoesNotContain(((char)0x00C2).ToString(), content);
        Assert.DoesNotContain('\uFFFD', content);
    }

    private sealed class AuthorizationFactory : WebApplicationFactory<Program>
    {
        public const string Password = "LocalOnly!12345";
        public static readonly string ContentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RealEstateManagement.Web"));

        private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"admin-authz-{Guid.NewGuid():N}.db");

        public static async Task<AuthorizationFactory> CreateAsync()
        {
            var factory = new AuthorizationFactory();
            _ = factory.Services;
            await factory.InitializeAsync();
            return factory;
        }

        public async Task<Guid> FindUserIdAsync(string email)
        {
            await using var scope = Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return (await userManager.FindByEmailAsync(email))!.Id;
        }

        public async Task<int> CountLeadsAssignedToAsync(Guid userId)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.Leads.CountAsync(lead => lead.AssignedToUserId == userId);
        }

        public async Task<Guid> FindPropertyIdAsync(string code)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.Properties.Where(property => property.Code == code).Select(property => property.Id).SingleAsync();
        }

        public async Task<Guid> FindLandlordContractIdAsync(Guid propertyId)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.LandlordContracts.Where(contract => contract.PropertyId == propertyId).Select(contract => contract.Id).SingleAsync();
        }

        public async Task<Guid> FindTenantContractIdAsync(Guid propertyId)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.TenantContracts.Where(contract => contract.PropertyId == propertyId).Select(contract => contract.Id).SingleAsync();
        }

        public async Task<Guid> CreateLeadAsync(string name, string contact, Guid? assignedToUserId)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var lead = new Lead(Guid.NewGuid(), name, contact, null, "Test", "Test message", "vi", DateTimeOffset.UtcNow);
            if (assignedToUserId is not null)
            {
                lead.AssignTo(assignedToUserId.Value, DateTimeOffset.UtcNow);
            }

            dbContext.Leads.Add(lead);
            await dbContext.SaveChangesAsync();
            return lead.Id;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseContentRoot(ContentRoot);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath};Pooling=False"
                });
            });
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                var keysPath = Path.Combine(Path.GetTempPath(), "RealEstateManagement.Tests.DataProtectionKeys");
                Directory.CreateDirectory(keysPath);
                services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }

        private async Task InitializeAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            foreach (var role in ApplicationRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }
            }

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await CreateUserAsync(userManager, "owner@example.test", ApplicationRoles.Owner);
            await CreateUserAsync(userManager, "staff@example.test", ApplicationRoles.Staff);
            await scope.ServiceProvider.GetRequiredService<DevelopmentRealEstateSeeder>().SeedAsync();
        }

        private static async Task CreateUserAsync(UserManager<ApplicationUser> userManager, string email, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    FullName = email,
                    DisplayName = email,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                };
                var result = await userManager.CreateAsync(user, Password);
                Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);
                Assert.True(roleResult.Succeeded, string.Join(", ", roleResult.Errors.Select(error => error.Description)));
            }
        }
    }
}

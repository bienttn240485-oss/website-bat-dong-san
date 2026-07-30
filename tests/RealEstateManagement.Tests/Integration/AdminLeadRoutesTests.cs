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

public sealed class AdminLeadRoutesTests
{
    [Fact]
    public async Task ContactPost_WhenAnonymousAndValid_CreatesLead()
    {
        await using var factory = await AdminLeadFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client, "/contact");
        var contact = $"0909{Random.Shared.Next(100000, 999999)}";

        var response = await client.PostAsync("/contact", Form([
            ("__RequestVerificationToken", token),
            ("Name", "Khach Public"),
            ("Contact", contact),
            ("Subject", "Tu van"),
            ("Message", "Can xem can ho"),
            ("Language", "vi")
        ]));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lead = await dbContext.Leads.SingleAsync(item => item.Contact == contact);
        Assert.Equal(LeadStatus.New, lead.Status);
        Assert.Null(lead.AssignedToUserId);
    }

    [Fact]
    public async Task ContactPost_WhenInvalid_ShowsValidation()
    {
        await using var factory = await AdminLeadFactory.CreateAsync();
        using var client = factory.CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/contact");

        var response = await client.PostAsync("/contact", Form([
            ("__RequestVerificationToken", token),
            ("Name", ""),
            ("Contact", "")
        ]));
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Vui lòng nhập tên của bạn.", content);
        Assert.Contains("Vui lòng nhập thông tin liên hệ.", content);
    }

    [Fact]
    public async Task ContactPost_WhenClientPostsSensitiveFields_IgnoresThem()
    {
        await using var factory = await AdminLeadFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client, "/contact");
        var staffId = await factory.FindUserIdAsync("staff@example.test");
        var contact = $"0910{Random.Shared.Next(100000, 999999)}";

        await client.PostAsync("/contact", Form([
            ("__RequestVerificationToken", token),
            ("Name", "Khach Public"),
            ("Contact", contact),
            ("Status", "Converted"),
            ("AssignedToUserId", staffId.ToString())
        ]));

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lead = await dbContext.Leads.SingleAsync(item => item.Contact == contact);
        Assert.Equal(LeadStatus.New, lead.Status);
        Assert.Null(lead.AssignedToUserId);
    }

    [Fact]
    public async Task AdminLeads_WhenAnonymous_RedirectsToLogin()
    {
        await using var factory = await AdminLeadFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/admin/leads");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task AdminLeads_WhenOwnerAuthenticated_RendersListAndDetail()
    {
        await using var factory = await AdminLeadFactory.CreateAsync();
        var leadId = await factory.CreateLeadAsync("Lead Admin", "lead-admin@example.test");
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminLeadFactory.Password);

        var list = await client.GetAsync("/admin/leads");
        var listContent = await ReadDecodedContentAsync(list);
        var detail = await client.GetAsync($"/admin/leads/{leadId}");
        var detailContent = await ReadDecodedContentAsync(detail);

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains("Lead Admin", listContent);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Contains("Lead Admin", detailContent);
        Assert.Contains("Mới", detailContent);
    }

    [Fact]
    public async Task AdminLeadDetail_WhenMissing_ReturnsNotFound()
    {
        await using var factory = await AdminLeadFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminLeadFactory.Password);

        var response = await client.GetAsync($"/admin/leads/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StatusAndAssign_WhenAntiforgeryMissing_AreRejected()
    {
        await using var factory = await AdminLeadFactory.CreateAsync();
        var leadId = await factory.CreateLeadAsync("Lead Token", "lead-token@example.test");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "owner@example.test", AdminLeadFactory.Password);

        var status = await client.PostAsync($"/admin/leads/{leadId}/status", Form([("Status", LeadStatus.Contacted.ToString())]));
        var assign = await client.PostAsync($"/admin/leads/{leadId}/assign", Form([("SaleUserId", (await factory.FindUserIdAsync("staff@example.test")).ToString())]));

        Assert.Equal(HttpStatusCode.BadRequest, status.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, assign.StatusCode);
    }

    [Fact]
    public async Task StatusAndAssign_WhenOwnerPostsValid_UpdatesLead()
    {
        await using var factory = await AdminLeadFactory.CreateAsync();
        var leadId = await factory.CreateLeadAsync("Lead Update", "lead-update@example.test");
        var staffId = await factory.FindUserIdAsync("staff@example.test");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "owner@example.test", AdminLeadFactory.Password);
        var token = await GetAntiforgeryTokenAsync(client, $"/admin/leads/{leadId}");

        var assign = await client.PostAsync($"/admin/leads/{leadId}/assign", Form([
            ("__RequestVerificationToken", token),
            ("SaleUserId", staffId.ToString())
        ]));
        token = await GetAntiforgeryTokenAsync(client, $"/admin/leads/{leadId}");
        var status = await client.PostAsync($"/admin/leads/{leadId}/status", Form([
            ("__RequestVerificationToken", token),
            ("Status", LeadStatus.Contacted.ToString())
        ]));

        Assert.Equal(HttpStatusCode.Redirect, assign.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, status.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lead = await dbContext.Leads.SingleAsync(item => item.Id == leadId);
        Assert.Equal(staffId, lead.AssignedToUserId);
        Assert.Equal(LeadStatus.Contacted, lead.Status);
    }

    [Fact]
    public async Task Staff_WhenLeadNotAssigned_IsBlocked()
    {
        await using var factory = await AdminLeadFactory.CreateAsync();
        var leadId = await factory.CreateLeadAsync("Lead Other", "lead-other@example.test");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "staff@example.test", AdminLeadFactory.Password);

        var response = await client.GetAsync($"/admin/leads/{leadId}");

        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task PropertyDetail_WhenLeadExists_RendersRelatedLead()
    {
        await using var factory = await AdminLeadFactory.CreateAsync();
        var propertyId = await factory.FindPropertyIdAsync("OP-0101");
        await factory.CreateLeadAsync("Lead Property", "lead-property@example.test", propertyId);
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminLeadFactory.Password);

        var response = await client.GetAsync($"/admin/properties/{propertyId}");
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Lead quan tâm", content);
        Assert.Contains("Lead Property", content);
    }

    [Fact]
    public async Task NewLeadViews_DoNotContainMojibake()
    {
        var root = Path.Combine(AdminLeadFactory.ContentRoot, "Areas", "Admin", "Views", "Leads");
        foreach (var path in Directory.GetFiles(root, "*.cshtml"))
        {
            AssertNoMojibake(await File.ReadAllTextAsync(path));
        }

        AssertNoMojibake(await File.ReadAllTextAsync(Path.Combine(AdminLeadFactory.ContentRoot, "Views", "Home", "Contact.cshtml")));
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

    private sealed class AdminLeadFactory : WebApplicationFactory<Program>
    {
        public const string Password = "LocalOnly!12345";
        public static readonly string ContentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RealEstateManagement.Web"));

        private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"admin-lead-{Guid.NewGuid():N}.db");

        public static async Task<AdminLeadFactory> CreateAsync()
        {
            var factory = new AdminLeadFactory();
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

        public async Task<Guid> FindPropertyIdAsync(string code)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.Properties.Where(property => property.Code == code).Select(property => property.Id).SingleAsync();
        }

        public async Task<Guid> CreateLeadAsync(string name, string contact, Guid? propertyId = null, Guid? assignedToUserId = null)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var lead = new Lead(Guid.NewGuid(), name, contact, propertyId, "Test", "Test message", "vi", DateTimeOffset.UtcNow);
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

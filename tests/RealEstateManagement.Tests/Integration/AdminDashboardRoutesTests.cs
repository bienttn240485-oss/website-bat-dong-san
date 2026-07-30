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
using RealEstateManagement.Infrastructure.Data;
using RealEstateManagement.Infrastructure.Identity;
using RealEstateManagement.Infrastructure.SeedData;

namespace RealEstateManagement.Tests.Integration;

public sealed class AdminDashboardRoutesTests
{
    [Fact]
    public async Task Dashboard_WhenOwnerAuthenticated_RendersRealEstateMetrics()
    {
        await using var factory = await AdminDashboardFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminDashboardFactory.Password);

        var response = await client.GetAsync("/admin/dashboard");
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Vận hành bất động sản", content);
        Assert.Contains("Tổng căn hộ", content);
        Assert.Contains("Tài chính vận hành", content);
        Assert.Contains("GMV 12 tháng gần nhất", content);
        Assert.DoesNotContain("Đặt sân", content);
    }

    [Fact]
    public async Task Dashboard_WhenStaffAuthenticated_DoesNotRenderFinancialDetails()
    {
        await using var factory = await AdminDashboardFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "staff@example.test", AdminDashboardFactory.Password);

        var response = await client.GetAsync("/admin/dashboard");
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Tài khoản hiện tại không có quyền xem số liệu tài chính nhạy cảm.", content);
        Assert.DoesNotContain("Tổng giá vào/tháng", content);
        Assert.DoesNotContain("data-chart-url=\"/admin/api/dashboard/gmv\"", content);
    }

    [Fact]
    public async Task DashboardView_DoesNotContainMojibake()
    {
        var path = Path.Combine(AdminDashboardFactory.ContentRoot, "Areas", "Admin", "Views", "Dashboard", "Index.cshtml");
        var content = await File.ReadAllTextAsync(path);

        AssertNoMojibake(content);
    }

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

    private static FormUrlEncodedContent Form(IEnumerable<(string Key, string Value)> values)
        => new(values.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value)));

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

    private sealed class AdminDashboardFactory : WebApplicationFactory<Program>
    {
        public const string Password = "LocalOnly!12345";
        public static readonly string ContentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RealEstateManagement.Web"));

        private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"admin-dashboard-{Guid.NewGuid():N}.db");

        public static async Task<AdminDashboardFactory> CreateAsync()
        {
            var factory = new AdminDashboardFactory();
            _ = factory.Services;
            await factory.InitializeAsync();
            return factory;
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

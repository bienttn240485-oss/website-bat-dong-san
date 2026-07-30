using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RealEstateManagement.Application.Common.Security;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Properties;
using RealEstateManagement.Infrastructure.Data;
using RealEstateManagement.Infrastructure.Identity;
using RealEstateManagement.Infrastructure.SeedData;

namespace RealEstateManagement.Tests.Integration;

public sealed class AdminPropertyRoutesTests
{
    [Fact]
    public async Task Properties_WhenAnonymous_RedirectsToLogin()
    {
        await using var factory = await AdminPropertyFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/admin/properties");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Properties_WhenOwnerAuthenticated_ReturnsSeededListWithVietnameseLabels()
    {
        await using var factory = await AdminPropertyFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminPropertyFactory.Password);

        var response = await client.GetAsync("/admin/properties");
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Căn hộ", content);
        Assert.Contains("OP-0101", content);
        Assert.Contains("ORI-1808", content);
        Assert.Contains("Đang trống", content);
        Assert.Contains("Đã thuê", content);
        Assert.Contains("Sắp trống", content);
        Assert.Contains("5.600.000.000 ₫", content);
        AssertNoMojibake(content);
    }

    [Fact]
    public async Task Create_WhenInvalid_ReturnsValidationMessages()
    {
        await using var factory = await AdminPropertyFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminPropertyFactory.Password);

        var token = await GetAntiforgeryTokenAsync(client, "/admin/properties/create");
        var response = await client.PostAsync("/admin/properties/create", Form([
            ("__RequestVerificationToken", token),
            ("Code", ""),
            ("Area", ""),
            ("Type", "TwoBedroomOneBathroom"),
            ("Status", "Available"),
            ("AreaSize", "-1"),
            ("Bathrooms", "-1"),
            ("MonthlyPrice", "-1")
        ]));
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Vui lòng nhập mã căn hộ.", content);
        Assert.Contains("Vui lòng nhập phân khu.", content);
        Assert.Contains("Số WC không được âm.", content);
        Assert.Contains("Giá thuê không được âm.", content);
    }

    [Fact]
    public async Task Create_WhenValid_PersistsProperty()
    {
        await using var factory = await AdminPropertyFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "owner@example.test", AdminPropertyFactory.Password);
        var code = $"PX-{Guid.NewGuid():N}"[..10].ToUpperInvariant();

        var token = await GetAntiforgeryTokenAsync(client, "/admin/properties/create");
        var response = await client.PostAsync("/admin/properties/create", ValidPropertyForm(token, code, "Glory Heights"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await dbContext.Properties.AnyAsync(property => property.Code == code && property.Area == "Glory Heights"));
    }

    [Fact]
    public async Task Create_WhenCodeDuplicated_ShowsBusinessError()
    {
        await using var factory = await AdminPropertyFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminPropertyFactory.Password);

        var token = await GetAntiforgeryTokenAsync(client, "/admin/properties/create");
        var response = await client.PostAsync("/admin/properties/create", ValidPropertyForm(token, "OP-0101", "S1"));
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Mã căn hộ đã tồn tại.", content);
    }

    [Fact]
    public async Task Edit_WhenValid_UpdatesProperty()
    {
        await using var factory = await AdminPropertyFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "owner@example.test", AdminPropertyFactory.Password);
        var propertyId = await factory.FindPropertyIdAsync("ORI-1808");

        var token = await GetAntiforgeryTokenAsync(client, $"/admin/properties/{propertyId}/edit");
        var response = await client.PostAsync($"/admin/properties/{propertyId}/edit", ValidPropertyForm(token, "ORI-1808", "Origami S9"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal("Origami S9", (await dbContext.Properties.SingleAsync(property => property.Id == propertyId)).Area);
    }

    [Fact]
    public async Task Detail_WhenPropertyMissing_ReturnsNotFound()
    {
        await using var factory = await AdminPropertyFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminPropertyFactory.Password);

        var response = await client.GetAsync($"/admin/properties/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenAntiforgeryMissing_IsRejected()
    {
        await using var factory = await AdminPropertyFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "owner@example.test", AdminPropertyFactory.Password);
        var propertyId = await factory.FindPropertyIdAsync("ORI-1808");

        var response = await client.PostAsync($"/admin/properties/{propertyId}/delete", Form([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenPropertyHasContract_DoesNotDelete()
    {
        await using var factory = await AdminPropertyFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminPropertyFactory.Password);
        var propertyId = await factory.FindPropertyIdAsync("OP-0101");
        var token = await GetAntiforgeryTokenAsync(client, $"/admin/properties/{propertyId}");

        var response = await client.PostAsync($"/admin/properties/{propertyId}/delete", Form([
            ("__RequestVerificationToken", token)
        ]));
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Không thể xóa căn hộ đang có hợp đồng chủ nhà hoặc hợp đồng thuê.", content);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await dbContext.Properties.AnyAsync(property => property.Id == propertyId));
    }

    [Fact]
    public async Task Delete_WhenStaffAuthenticated_IsBlocked()
    {
        await using var factory = await AdminPropertyFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "staff@example.test", AdminPropertyFactory.Password);
        var propertyId = await factory.FindPropertyIdAsync("ORI-1808");
        var token = await GetAntiforgeryTokenAsync(client, $"/admin/properties/{propertyId}");

        var response = await client.PostAsync($"/admin/properties/{propertyId}/delete", Form([
            ("__RequestVerificationToken", token)
        ]));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NewPropertyViews_DoNotContainMojibake()
    {
        var viewPaths = Directory.GetFiles(
            Path.Combine(AdminPropertyFactory.ContentRoot, "Areas", "Admin", "Views", "Properties"),
            "*.cshtml");

        foreach (var path in viewPaths)
        {
            AssertNoMojibake(await File.ReadAllTextAsync(path));
        }
    }

    private static FormUrlEncodedContent ValidPropertyForm(string token, string code, string area)
        => Form([
            ("__RequestVerificationToken", token),
            ("Code", code),
            ("Project", "GloryHeights"),
            ("Area", area),
            ("Type", "TwoBedroomTwoBathrooms"),
            ("AreaSize", "70"),
            ("Bathrooms", "2"),
            ("MonthlyPrice", "18000000"),
            ("SalePrice", "5200000000"),
            ("Direction", "Đông Nam"),
            ("LoanInfo", "Hỗ trợ vay ngân hàng"),
            ("LegalStatus", "Sổ hồng"),
            ("FurniturePackage", "Nội thất cơ bản"),
            ("Description", "Căn hộ test cho Admin Property."),
            ("VideoUrl", "https://example.test/video"),
            ("Status", "Available"),
            ("AvailableFromDate", "2026-09-01"),
            ("Notes", "Dữ liệu kiểm thử"),
            ("ImagesText", "/images/properties/test.jpg"),
            ("FurnitureText", "Sofa | 1"),
            ("AmenitiesText", "Hồ bơi")
        ]);

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

    private sealed class AdminPropertyFactory : WebApplicationFactory<Program>
    {
        public const string Password = "LocalOnly!12345";
        public static readonly string ContentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RealEstateManagement.Web"));

        private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"admin-property-{Guid.NewGuid():N}.db");

        public static async Task<AdminPropertyFactory> CreateAsync()
        {
            var factory = new AdminPropertyFactory();
            _ = factory.Services;
            await factory.InitializeAsync();
            return factory;
        }

        public async Task<Guid> FindPropertyIdAsync(string code)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.Properties
                .Where(property => property.Code == code)
                .Select(property => property.Id)
                .SingleAsync();
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

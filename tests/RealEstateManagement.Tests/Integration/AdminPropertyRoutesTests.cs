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
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        Assert.Contains("C\u0103n h\u1ed9", content);
        Assert.Contains("OP-A-0901", content);
        Assert.Contains("OR-S7-1002", content);
        Assert.Contains("\u0110ang tr\u1ed1ng", content);
        Assert.Contains("\u0110\u00e3 thu\u00ea", content);
        Assert.Contains("S\u1eafp tr\u1ed1ng", content);
        Assert.Contains("5.200.000.000 \u20ab", content);
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
        Assert.Contains("Vui l\u00f2ng nh\u1eadp m\u00e3 c\u0103n h\u1ed9.", content);
        Assert.Contains("Vui l\u00f2ng nh\u1eadp ph\u00e2n khu.", content);
        Assert.Contains("S\u1ed1 WC kh\u00f4ng \u0111\u01b0\u1ee3c \u00e2m.", content);
        Assert.Contains("Gi\u00e1 thu\u00ea kh\u00f4ng \u0111\u01b0\u1ee3c \u00e2m.", content);
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
        var response = await client.PostAsync("/admin/properties/create", ValidPropertyForm(token, "OP-A-0901", "S1"));
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("M\u00e3 c\u0103n h\u1ed9 \u0111\u00e3 t\u1ed3n t\u1ea1i.", content);
    }

    [Fact]
    public async Task Edit_WhenValid_UpdatesProperty()
    {
        await using var factory = await AdminPropertyFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "owner@example.test", AdminPropertyFactory.Password);
        var propertyId = await factory.FindPropertyIdAsync("OR-S7-1002");

        var token = await GetAntiforgeryTokenAsync(client, $"/admin/properties/{propertyId}/edit");
        var response = await client.PostAsync($"/admin/properties/{propertyId}/edit", ValidPropertyForm(token, "OR-S7-1002", "Origami S9"));

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
        var propertyId = await factory.FindPropertyIdAsync("OR-S7-1002");

        var response = await client.PostAsync($"/admin/properties/{propertyId}/delete", Form([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenPropertyHasContract_DoesNotDelete()
    {
        await using var factory = await AdminPropertyFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminPropertyFactory.Password);
        var propertyId = await factory.FindPropertyIdAsync("OP-A-0901");
        var token = await GetAntiforgeryTokenAsync(client, $"/admin/properties/{propertyId}");

        var response = await client.PostAsync($"/admin/properties/{propertyId}/delete", Form([
            ("__RequestVerificationToken", token)
        ]));
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Kh\u00f4ng th\u1ec3 x\u00f3a c\u0103n h\u1ed9 \u0111ang c\u00f3 h\u1ee3p \u0111\u1ed3ng ch\u1ee7 nh\u00e0 ho\u1eb7c h\u1ee3p \u0111\u1ed3ng thu\u00ea.", content);

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
        var propertyId = await factory.FindPropertyIdAsync("OR-S7-1002");
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
            ("Direction", "\u0110\u00f4ng Nam"),
            ("LoanInfo", "H\u1ed7 tr\u1ee3 vay ng\u00e2n h\u00e0ng"),
            ("LegalStatus", "S\u1ed5 h\u1ed3ng"),
            ("FurniturePackage", "N\u1ed9i th\u1ea5t c\u01a1 b\u1ea3n"),
            ("Description", "C\u0103n h\u1ed9 test cho Admin Property."),
            ("VideoUrl", "https://example.test/video"),
            ("Status", "Available"),
            ("AvailableFromDate", "2026-09-01"),
            ("Notes", "D\u1eef li\u1ec7u ki\u1ec3m th\u1eed"),
            ("ImagesText", "/images/properties/test.jpg"),
            ("FurnitureText", "Sofa | 1"),
            ("AmenitiesText", "H\u1ed3 b\u01a1i")
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
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={databasePath};Pooling=False"));
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
            var user = await userManager.FindByEmailAsync(email) ?? await userManager.FindByNameAsync(email);
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



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
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Properties;
using RealEstateManagement.Infrastructure.Data;
using RealEstateManagement.Infrastructure.Identity;
using RealEstateManagement.Infrastructure.SeedData;

namespace RealEstateManagement.Tests.Integration;

public sealed class AdminContractRoutesTests
{
    [Theory]
    [InlineData("/admin/landlord-contracts")]
    [InlineData("/admin/tenant-contracts")]
    public async Task ContractRoutes_WhenAnonymous_RedirectToLogin(string route)
    {
        await using var factory = await AdminContractFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task LandlordContracts_WhenOwnerAuthenticated_RendersListWithVietnameseLabels()
    {
        await using var factory = await AdminContractFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminContractFactory.Password);

        var response = await client.GetAsync("/admin/landlord-contracts");
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Hợp đồng chủ nhà", content);
        Assert.Contains("OP-0101", content);
        Assert.Contains("Chưa bổ sung", content);
    }

    [Fact]
    public async Task TenantContracts_WhenOwnerAuthenticated_RendersListWithVietnameseLabels()
    {
        await using var factory = await AdminContractFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminContractFactory.Password);

        var response = await client.GetAsync("/admin/tenant-contracts");
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Hợp đồng khách thuê", content);
        Assert.Contains("OP-0101", content);
        Assert.Contains("Đang hiệu lực", content);
    }

    [Fact]
    public async Task LandlordCreate_WhenInvalid_ReturnsValidation()
    {
        await using var factory = await AdminContractFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminContractFactory.Password);

        var token = await GetAntiforgeryTokenAsync(client, "/admin/landlord-contracts/create");
        var response = await client.PostAsync("/admin/landlord-contracts/create", Form([
            ("__RequestVerificationToken", token),
            ("PropertyId", ""),
            ("LandlordName", ""),
            ("InputPrice", "-1"),
            ("SignedDate", "2026-07-01"),
            ("ExpiryDate", "2026-06-01"),
            ("DepositStatus", "Pending")
        ]));
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Vui lòng chọn căn hộ.", content);
        Assert.Contains("Vui lòng nhập tên chủ nhà.", content);
    }

    [Fact]
    public async Task TenantCreate_WhenValid_PersistsContractAndUpdatesProperty()
    {
        await using var factory = await AdminContractFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "owner@example.test", AdminContractFactory.Password);
        var propertyId = await factory.CreateAvailablePropertyAsync();

        var token = await GetAntiforgeryTokenAsync(client, $"/admin/tenant-contracts/create?propertyId={propertyId}");
        var response = await client.PostAsync("/admin/tenant-contracts/create", TenantForm(token, propertyId, "Khach Thue Test", 21_000_000));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await dbContext.TenantContracts.AnyAsync(contract => contract.PropertyId == propertyId && contract.TenantName == "Khach Thue Test"));
    }

    [Fact]
    public async Task TenantCreate_WhenOverlaps_ShowsBusinessError()
    {
        await using var factory = await AdminContractFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminContractFactory.Password);
        var propertyId = await factory.FindPropertyIdAsync("OP-0101");

        var token = await GetAntiforgeryTokenAsync(client, $"/admin/tenant-contracts/create?propertyId={propertyId}");
        var response = await client.PostAsync("/admin/tenant-contracts/create", TenantForm(token, propertyId, "Khach Trung Lich", 22_000_000));
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Căn hộ đã có hợp đồng khách thuê đang hiệu lực trong khoảng thời gian này.", content);
    }

    [Fact]
    public async Task TenantEdit_WhenValid_UpdatesContract()
    {
        await using var factory = await AdminContractFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "owner@example.test", AdminContractFactory.Password);
        var contractId = await factory.FindTenantContractIdAsync("OP-0101");
        var propertyId = await factory.FindPropertyIdAsync("OP-0101");

        var token = await GetAntiforgeryTokenAsync(client, $"/admin/tenant-contracts/{contractId}/edit");
        var response = await client.PostAsync($"/admin/tenant-contracts/{contractId}/edit", TenantForm(token, propertyId, "Khach Cap Nhat", 25_000_000));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal("Khach Cap Nhat", (await dbContext.TenantContracts.SingleAsync(contract => contract.Id == contractId)).TenantName);
    }

    [Fact]
    public async Task TenantDetail_WhenMissing_ReturnsNotFound()
    {
        await using var factory = await AdminContractFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminContractFactory.Password);

        var response = await client.GetAsync($"/admin/tenant-contracts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TenantDelete_WhenAntiforgeryMissing_IsRejected()
    {
        await using var factory = await AdminContractFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "owner@example.test", AdminContractFactory.Password);
        var contractId = await factory.FindTenantContractIdAsync("OP-0101");

        var response = await client.PostAsync($"/admin/tenant-contracts/{contractId}/delete", Form([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TenantDelete_WhenRequested_DoesNotCascadeOrDeleteContract()
    {
        await using var factory = await AdminContractFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminContractFactory.Password);
        var contractId = await factory.FindTenantContractIdAsync("OP-0101");
        var token = await GetAntiforgeryTokenAsync(client, $"/admin/tenant-contracts/{contractId}");

        var response = await client.PostAsync($"/admin/tenant-contracts/{contractId}/delete", Form([("__RequestVerificationToken", token)]));
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Không xóa hợp đồng khách thuê để giữ lịch sử quản lý.", content);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await dbContext.TenantContracts.AnyAsync(contract => contract.Id == contractId));
        Assert.True(await dbContext.Properties.AnyAsync(property => property.Code == "OP-0101"));
    }

    [Fact]
    public async Task TenantStatus_WhenStaffAuthenticated_IsBlocked()
    {
        await using var factory = await AdminContractFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "staff@example.test", AdminContractFactory.Password);
        var contractId = await factory.FindTenantContractIdAsync("OP-0101");
        var token = await GetAntiforgeryTokenAsync(client, $"/admin/tenant-contracts/{contractId}");

        var response = await client.PostAsync($"/admin/tenant-contracts/{contractId}/status", Form([
            ("__RequestVerificationToken", token),
            ("status", ContractStatus.Cancelled.ToString())
        ]));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task PropertyDetail_RendersContractFinancialSummary()
    {
        await using var factory = await AdminContractFactory.CreateAsync();
        using var client = factory.CreateClient();
        await LoginAsync(client, "owner@example.test", AdminContractFactory.Password);
        var propertyId = await factory.FindPropertyIdAsync("OP-0101");

        var response = await client.GetAsync($"/admin/properties/{propertyId}");
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Hợp đồng và tài chính", content);
        Assert.Contains("Chênh lệch/tháng", content);
        Assert.Contains("Chênh lệch dự kiến/năm", content);
    }

    [Fact]
    public async Task NewContractViews_DoNotContainMojibake()
    {
        foreach (var directory in new[] { "LandlordContracts", "TenantContracts" })
        {
            var viewPaths = Directory.GetFiles(
                Path.Combine(AdminContractFactory.ContentRoot, "Areas", "Admin", "Views", directory),
                "*.cshtml");

            foreach (var path in viewPaths)
            {
                AssertNoMojibake(await File.ReadAllTextAsync(path));
            }
        }
    }

    private static FormUrlEncodedContent TenantForm(string token, Guid propertyId, string tenantName, long rentalPrice)
        => Form([
            ("__RequestVerificationToken", token),
            ("PropertyId", propertyId.ToString()),
            ("TenantName", tenantName),
            ("ManagerName", "Sale Test"),
            ("RentalPrice", rentalPrice.ToString()),
            ("SignedDate", "2026-08-01"),
            ("TermMonths", "12"),
            ("DepositAmount", "42000000"),
            ("DepositReturnDate", ""),
            ("PeCode", "PE-TEST"),
            ("PassCode", "1234"),
            ("Status", "Active"),
            ("Notes", "Hop dong test")
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

    private sealed class AdminContractFactory : WebApplicationFactory<Program>
    {
        public const string Password = "LocalOnly!12345";
        public static readonly string ContentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RealEstateManagement.Web"));

        private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"admin-contract-{Guid.NewGuid():N}.db");

        public static async Task<AdminContractFactory> CreateAsync()
        {
            var factory = new AdminContractFactory();
            _ = factory.Services;
            await factory.InitializeAsync();
            return factory;
        }

        public async Task<Guid> FindPropertyIdAsync(string code)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.Properties.Where(property => property.Code == code).Select(property => property.Id).SingleAsync();
        }

        public async Task<Guid> FindTenantContractIdAsync(string propertyCode)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.TenantContracts
                .Where(contract => dbContext.Properties.Any(property => property.Id == contract.PropertyId && property.Code == propertyCode))
                .Select(contract => contract.Id)
                .SingleAsync();
        }

        public async Task<Guid> CreateAvailablePropertyAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var propertyId = Guid.NewGuid();
            var code = $"TC-{Guid.NewGuid():N}"[..10].ToUpperInvariant();
            dbContext.Properties.Add(new Property(
                propertyId,
                code,
                PropertyProject.GloryHeights,
                "Test Contract",
                PropertyType.TwoBedroomTwoBathrooms,
                72m,
                2,
                20_000_000,
                null,
                "Đông Nam",
                null,
                "Sổ hồng",
                "Nội thất cơ bản",
                "Căn hộ test hợp đồng khách thuê.",
                null,
                PropertyStatus.Available,
                null,
                null,
                DateTimeOffset.UtcNow));
            await dbContext.SaveChangesAsync();
            return propertyId;
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

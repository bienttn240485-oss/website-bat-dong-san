using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Infrastructure.Data;
using RealEstateManagement.Infrastructure.SeedData;

namespace RealEstateManagement.Tests.Integration;

public sealed class PublicPropertyRoutesTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/properties")]
    [InlineData("/sales")]
    [InlineData("/contact")]
    public async Task PublicRoutes_ReturnOk(string path)
    {
        await using var factory = await PublicRouteFactory.CreateAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RentalList_RendersSeedPropertiesAndVietnameseLabels()
    {
        await using var factory = await PublicRouteFactory.CreateAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/properties");
        var content = await ReadDecodedContentAsync(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ORI-***08", content);
        Assert.Contains("GH-***12", content);
        Assert.Contains("Đang trống", content);
        Assert.Contains("15.000.000 ₫/tháng", content);
    }

    [Fact]
    public async Task SaleList_HidesPropertiesWithoutSalePrice()
    {
        await using var factory = await PublicRouteFactory.CreateAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/sales");
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("OP-***01", content);
        Assert.DoesNotContain("GH-***12", content);
    }

    [Fact]
    public async Task RentalDetail_WhenExists_RendersPublicFields()
    {
        await using var factory = await PublicRouteFactory.CreateAsync();
        var propertyId = await factory.FindPropertyIdAsync("ORI-1808");
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/properties/{propertyId}");
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ORI-***08", content);
        Assert.Contains("Căn hộ", content);
        Assert.Contains("18.000.000", content);
        Assert.Contains("/tháng", content);
    }

    [Fact]
    public async Task DetailRoutes_WhenMissingOrInvalid_ReturnNotFound()
    {
        await using var factory = await PublicRouteFactory.CreateAsync();
        var noSaleId = await factory.FindPropertyIdAsync("GH-2312");
        using var client = factory.CreateClient();

        var missingRental = await client.GetAsync($"/properties/{Guid.NewGuid()}");
        var missingSale = await client.GetAsync($"/sales/{Guid.NewGuid()}");
        var saleWithoutPrice = await client.GetAsync($"/sales/{noSaleId}");

        Assert.Equal(HttpStatusCode.NotFound, missingRental.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingSale.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, saleWithoutPrice.StatusCode);
    }

    [Fact]
    public async Task PublicInquiry_WhenValid_CreatesLeadWithoutSensitiveBinding()
    {
        await using var factory = await PublicRouteFactory.CreateAsync();
        var propertyId = await factory.FindPropertyIdAsync("ORI-1808");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client, $"/properties/{propertyId}");
        var contact = $"0912{Random.Shared.Next(100000, 999999)}";

        var response = await client.PostAsync($"/properties/{propertyId}/inquiry", Form([
            ("__RequestVerificationToken", token),
            ("Name", "Khach Inquiry"),
            ("Contact", contact),
            ("Message", "Can xem can ho"),
            ("Language", "vi"),
            ("Status", "Converted"),
            ("AssignedToUserId", Guid.NewGuid().ToString())
        ]));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lead = await dbContext.Leads.SingleAsync(item => item.Contact == contact);
        Assert.Equal(propertyId, lead.PropertyId);
        Assert.Equal(LeadStatus.New, lead.Status);
        Assert.Null(lead.AssignedToUserId);
    }

    [Fact]
    public async Task PublicInquiry_WhenInvalid_ShowsValidation()
    {
        await using var factory = await PublicRouteFactory.CreateAsync();
        var propertyId = await factory.FindPropertyIdAsync("ORI-1808");
        using var client = factory.CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, $"/properties/{propertyId}");

        var response = await client.PostAsync($"/properties/{propertyId}/inquiry", Form([
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
    public async Task ContactForm_WhenValid_CreatesLead()
    {
        await using var factory = await PublicRouteFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client, "/contact");
        var contact = $"0913{Random.Shared.Next(100000, 999999)}";

        var response = await client.PostAsync("/contact", Form([
            ("__RequestVerificationToken", token),
            ("Name", "Khach Contact"),
            ("Contact", contact),
            ("Subject", "Tu van mua can"),
            ("Message", "Can lien he lai"),
            ("Language", "vi"),
            ("Status", "Converted"),
            ("AssignedToUserId", Guid.NewGuid().ToString())
        ]));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lead = await dbContext.Leads.SingleAsync(item => item.Contact == contact);
        Assert.Null(lead.PropertyId);
        Assert.Equal(LeadStatus.New, lead.Status);
        Assert.Null(lead.AssignedToUserId);
    }

    [Fact]
    public async Task PublicPages_DoNotExposeSensitiveContractData()
    {
        await using var factory = await PublicRouteFactory.CreateAsync();
        var propertyId = await factory.FindPropertyIdAsync("OP-0101");
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/sales/{propertyId}");
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Nguyễn Minh An", content);
        Assert.DoesNotContain("Lê Hoàng Nam", content);
        Assert.DoesNotContain("PE-OP-0101", content);
        Assert.DoesNotContain("2407", content);
        Assert.DoesNotContain("Giá nhập", content);
        Assert.DoesNotContain("Chủ nhà", content);
        Assert.DoesNotContain("Khách thuê", content);
        Assert.DoesNotContain("Ghi chú nội bộ", content);
    }

    [Fact]
    public async Task PublicNavbar_DoesNotContainMainBookingJourney()
    {
        await using var factory = await PublicRouteFactory.CreateAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        var content = await ReadDecodedContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("/booking", content);
        Assert.DoesNotContain("/fields", content);
        Assert.DoesNotContain("Đặt sân", content);
        Assert.DoesNotContain("Sân bóng", content);
    }

    [Fact]
    public async Task NewPublicViews_DoNotContainMojibake()
    {
        foreach (var path in new[]
        {
            Path.Combine(PublicRouteFactory.ContentRoot, "Views", "Home", "Index.cshtml"),
            Path.Combine(PublicRouteFactory.ContentRoot, "Views", "Home", "Contact.cshtml"),
            Path.Combine(PublicRouteFactory.ContentRoot, "Views", "Properties", "Index.cshtml"),
            Path.Combine(PublicRouteFactory.ContentRoot, "Views", "Properties", "Details.cshtml"),
            Path.Combine(PublicRouteFactory.ContentRoot, "Views", "Sales", "Index.cshtml"),
            Path.Combine(PublicRouteFactory.ContentRoot, "Views", "Sales", "Details.cshtml"),
            Path.Combine(PublicRouteFactory.ContentRoot, "Views", "Shared", "Partials", "_PublicNavbar.cshtml"),
            Path.Combine(PublicRouteFactory.ContentRoot, "Views", "Shared", "Partials", "_PublicFooter.cshtml")
        })
        {
            AssertNoMojibake(await File.ReadAllTextAsync(path));
        }
    }

    private static FormUrlEncodedContent Form(IEnumerable<(string Key, string Value)> values)
        => new(values.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value)));

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

    private sealed class PublicRouteFactory : WebApplicationFactory<Program>
    {
        public static readonly string ContentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RealEstateManagement.Web"));
        private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"public-route-{Guid.NewGuid():N}.db");

        public static async Task<PublicRouteFactory> CreateAsync()
        {
            var factory = new PublicRouteFactory();
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
            await scope.ServiceProvider.GetRequiredService<DevelopmentRealEstateSeeder>().SeedAsync();
        }
    }
}

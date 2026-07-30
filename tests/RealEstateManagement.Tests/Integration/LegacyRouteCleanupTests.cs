using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RealEstateManagement.Tests.Integration;

public sealed class LegacyRouteCleanupTests
{
    [Theory]
    [InlineData("/fields")]
    [InlineData("/fields/san-5a")]
    [InlineData("/booking")]
    [InlineData("/booking/lookup")]
    [InlineData("/booking-lookup")]
    [InlineData("/services")]
    [InlineData("/promotions")]
    [InlineData("/admin/bookings")]
    [InlineData("/admin/fields")]
    [InlineData("/admin/services")]
    [InlineData("/admin/promotions")]
    [InlineData("/admin/payments")]
    [InlineData("/admin/schedule")]
    [InlineData("/admin/api/schedule/events?start=2026-07-25&end=2026-08-01")]
    [InlineData("/admin/reports")]
    [InlineData("/admin/api/dashboard/revenue")]
    [InlineData("/admin/api/dashboard/bookings")]
    [InlineData("/admin/api/dashboard/utilization")]
    public async Task LegacyFootballRoutes_WhenRequested_ReturnNotFound(string path)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var contentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RealEstateManagement.Web"));

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseContentRoot(contentRoot);
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    var keysPath = Path.Combine(Path.GetTempPath(), "RealEstateManagement.Tests.LegacyRouteKeys");
                    Directory.CreateDirectory(keysPath);
                    services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));
                });
            });
    }
}

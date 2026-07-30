using System.Net;
using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Infrastructure.Data;
using RealEstateManagement.Infrastructure.SeedData;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace RealEstateManagement.Tests.Integration;

public sealed class FieldRoutesTests
{
    [Theory]
    [InlineData("/fields")]
    [InlineData("/fields/san-5a")]
    [InlineData("/booking")]
    [InlineData("/booking/lookup")]
    [InlineData("/services")]
    [InlineData("/promotions")]
    public async Task PublicLegacyFootballRoutes_WhenRequested_ReturnNotFound(string path)
    {
        await using var factory = await CreateSeededFactoryAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminFields_WhenAnonymous_RedirectsToAdminLogin()
    {
        await using var factory = await CreateSeededFactoryAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/admin/fields");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task AdminCommerceRoutes_WhenAnonymous_RedirectToAdminLogin()
    {
        await using var factory = await CreateSeededFactoryAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var paymentsResponse = await client.GetAsync("/admin/payments");
        var servicesResponse = await client.GetAsync("/admin/services");
        var promotionsResponse = await client.GetAsync("/admin/promotions");

        Assert.Equal(HttpStatusCode.Redirect, paymentsResponse.StatusCode);
        Assert.Equal("/admin/login", paymentsResponse.Headers.Location?.AbsolutePath);
        Assert.Equal(HttpStatusCode.Redirect, servicesResponse.StatusCode);
        Assert.Equal("/admin/login", servicesResponse.Headers.Location?.AbsolutePath);
        Assert.Equal(HttpStatusCode.Redirect, promotionsResponse.StatusCode);
        Assert.Equal("/admin/login", promotionsResponse.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task AdminBookings_WhenAnonymous_RedirectsToAdminLogin()
    {
        await using var factory = await CreateSeededFactoryAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/admin/bookings");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task AdminSchedule_WhenAnonymous_RedirectsToAdminLogin()
    {
        await using var factory = await CreateSeededFactoryAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var pageResponse = await client.GetAsync("/admin/schedule");
        var apiResponse = await client.GetAsync("/admin/api/schedule/events?start=2026-07-25&end=2026-08-01");

        Assert.Equal(HttpStatusCode.Redirect, pageResponse.StatusCode);
        Assert.Equal("/admin/login", pageResponse.Headers.Location?.AbsolutePath);
        Assert.Equal(HttpStatusCode.Redirect, apiResponse.StatusCode);
        Assert.Equal("/admin/login", apiResponse.Headers.Location?.AbsolutePath);
    }

    private static async Task<FieldRouteFactory> CreateSeededFactoryAsync()
    {
        var factory = new FieldRouteFactory();
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<DevelopmentFieldSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<DevelopmentCommerceSeeder>().SeedAsync();

        return factory;
    }

    private sealed class FieldRouteFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var contentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RealEstateManagement.Web"));

            _connection.Open();

            builder.UseEnvironment("Testing");
            builder.UseContentRoot(contentRoot);
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
                services.RemoveAll<ISystemClock>();
                services.AddSingleton<ISystemClock, FixedClock>();

                var keysPath = Path.Combine(Path.GetTempPath(), "RealEstateManagement.Tests.FieldRouteKeys");
                Directory.CreateDirectory(keysPath);
                services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _connection.Dispose();
        }
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => new(2026, 7, 25, 0, 0, 0, TimeSpan.Zero);
    }
}

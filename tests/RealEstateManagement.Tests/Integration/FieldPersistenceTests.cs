using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Fields;
using RealEstateManagement.Infrastructure.Data;
using RealEstateManagement.Infrastructure.Fields;
using RealEstateManagement.Infrastructure.SeedData;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace RealEstateManagement.Tests.Integration;

public sealed class FieldPersistenceTests
{
    [Fact]
    public async Task DevelopmentFieldSeeder_WhenRunTwice_CreatesThreeFieldsOnlyOnce()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var seeder = new DevelopmentFieldSeeder(dbContext, new FixedClock());

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Equal(3, await dbContext.Fields.CountAsync());
        Assert.Equal(21, await dbContext.FieldOperatingHours.CountAsync());
        Assert.Equal(9, await dbContext.PricingRules.CountAsync());
    }

    [Fact]
    public async Task EfFieldStore_ListPublicFields_ReturnsSeededVietnameseFields()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await new DevelopmentFieldSeeder(dbContext, new FixedClock()).SeedAsync();

        IFieldStore store = new EfFieldStore(dbContext);

        var fields = await store.ListPublicFieldsAsync(CancellationToken.None);

        Assert.Equal(["Sân 5A", "Sân 5B", "Sân 7A"], fields.Select(field => field.Name).Order().ToArray());
        Assert.All(fields, field => Assert.NotNull(field.PriceFrom));
    }

    [Fact]
    public async Task DevelopmentCommerceSeeder_WhenRunTwice_CreatesVietnameseServicesAndPromotionsOnce()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var seeder = new DevelopmentCommerceSeeder(dbContext, new FixedClock());

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Equal(4, await dbContext.Services.CountAsync());
        Assert.Equal(2, await dbContext.PromoCodes.CountAsync());
        Assert.Contains(await dbContext.Services.Select(service => service.Name).ToArrayAsync(), name => name == "Thuê trọng tài");
        Assert.Contains(await dbContext.PromoCodes.Select(promotion => promotion.Code).ToArrayAsync(), code => code == "ANPHU50");
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => new(2026, 7, 25, 0, 0, 0, TimeSpan.Zero);
    }
}


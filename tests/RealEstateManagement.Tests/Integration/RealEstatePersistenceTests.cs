using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Domain.Properties;
using RealEstateManagement.Domain.Users;
using RealEstateManagement.Infrastructure.Data;
using RealEstateManagement.Infrastructure.Identity;
using RealEstateManagement.Infrastructure.SeedData;
using RealEstateManagement.Application.Common.Time;

namespace RealEstateManagement.Tests.Integration;

public sealed class RealEstatePersistenceTests
{
    [Fact]
    public async Task PropertyAggregate_WhenSaved_PersistsRelationshipsAndDates()
    {
        await using var connection = await OpenInMemorySqliteAsync();
        var options = CreateOptions(connection);
        var now = new DateTimeOffset(2026, 7, 29, 3, 15, 0, TimeSpan.Zero);
        var property = CreateProperty("OP-2201", now);
        property.ReplaceImages([
            new PropertyImage(Guid.NewGuid(), property.Id, "/images/op-2201-1.jpg", "Ảnh phòng khách", 1, true),
            new PropertyImage(Guid.NewGuid(), property.Id, "/images/op-2201-2.jpg", "Ảnh phòng ngủ", 2, false)
        ]);

        await using (var dbContext = new ApplicationDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();
            dbContext.Properties.Add(property);
            dbContext.LandlordContracts.Add(new LandlordContract(
                Guid.NewGuid(),
                property.Id,
                "Nguyễn Thảo",
                "PE-2201",
                "Trần Minh",
                20_000_000,
                new DateOnly(2026, 7, 1),
                new DateOnly(2027, 7, 1),
                DepositStatus.Supplemented,
                5,
                "Ngày 1-5 hằng tháng",
                new DateOnly(2026, 8, 5),
                null,
                now));
            dbContext.TenantContracts.AddRange(
                new TenantContract(
                    Guid.NewGuid(),
                    property.Id,
                    "Lê Gia Hân",
                    "Trần Minh",
                    26_000_000,
                    new DateOnly(2026, 7, 15),
                    12,
                    52_000_000,
                    null,
                    "PE-2201",
                    "2201",
                    ContractStatus.Active,
                    null,
                    now),
                new TenantContract(
                    Guid.NewGuid(),
                    property.Id,
                    "Phạm Khánh",
                    "Trần Minh",
                    25_000_000,
                    new DateOnly(2025, 7, 15),
                    12,
                    50_000_000,
                    new DateOnly(2026, 7, 20),
                    "PE-OLD",
                    null,
                    ContractStatus.Expired,
                    null,
                    now));

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = new ApplicationDbContext(options))
        {
            var loaded = await dbContext.Properties
                .Include(item => item.Images)
                .SingleAsync(item => item.Code == "OP-2201");

            Assert.Equal(PropertyStatus.Available, loaded.Status);
            Assert.Equal(new DateOnly(2026, 8, 1), loaded.AvailableFromDate);
            Assert.Equal(now, loaded.CreatedAtUtc);
            Assert.Equal(2, loaded.Images.Count);
            Assert.Equal(2, await dbContext.TenantContracts.CountAsync(contract => contract.PropertyId == loaded.Id));
            Assert.NotNull(await dbContext.LandlordContracts.SingleAsync(contract => contract.PropertyId == loaded.Id));
        }
    }

    [Fact]
    public async Task PropertyCode_WhenDuplicated_IsRejectedByUniqueIndex()
    {
        await using var connection = await OpenInMemorySqliteAsync();
        var options = CreateOptions(connection);

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.Properties.Add(CreateProperty("ORI-0101", new DateTimeOffset(2026, 7, 29, 0, 0, 0, TimeSpan.Zero)));
        await dbContext.SaveChangesAsync();

        dbContext.Properties.Add(CreateProperty("ORI-0101", new DateTimeOffset(2026, 7, 29, 1, 0, 0, TimeSpan.Zero)));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Lead_WhenSaved_AllowsNoPropertyAndAssignedUser()
    {
        await using var connection = await OpenInMemorySqliteAsync();
        var options = CreateOptions(connection);
        var now = new DateTimeOffset(2026, 7, 29, 4, 0, 0, TimeSpan.Zero);
        var saleUserId = Guid.NewGuid();

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.Users.Add(new ApplicationUser
        {
            Id = saleUserId,
            UserName = "sale@example.test",
            NormalizedUserName = "SALE@EXAMPLE.TEST",
            Email = "sale@example.test",
            NormalizedEmail = "SALE@EXAMPLE.TEST",
            FullName = "Sale Test",
            DisplayName = "Sale",
            AccountStatus = AccountStatus.Active,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        var unassigned = new Lead(Guid.NewGuid(), "Ngọc Anh", "0909 000 111", null, "Tìm căn thuê", null, "vi", now);
        var assigned = new Lead(Guid.NewGuid(), "Hoàng Bảo", "0909 000 222", null, "Tư vấn mua", null, "vi", now);
        assigned.AssignTo(saleUserId, now);
        dbContext.Leads.AddRange(unassigned, assigned);
        await dbContext.SaveChangesAsync();

        var leads = await dbContext.Leads.OrderBy(lead => lead.Contact).ToListAsync();

        Assert.Null(leads[0].PropertyId);
        Assert.Null(leads[0].AssignedToUserId);
        Assert.Equal(saleUserId, leads[1].AssignedToUserId);
    }

    [Fact]
    public async Task DeleteProperty_WhenContractsExist_IsRestricted()
    {
        await using var connection = await OpenInMemorySqliteAsync();
        var options = CreateOptions(connection);
        var now = new DateTimeOffset(2026, 7, 29, 5, 0, 0, TimeSpan.Zero);
        var property = CreateProperty("RES-0909", now);

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.Properties.Add(property);
        dbContext.TenantContracts.Add(new TenantContract(
            Guid.NewGuid(),
            property.Id,
            "Đỗ Minh",
            null,
            18_000_000,
            new DateOnly(2026, 8, 1),
            12,
            36_000_000,
            null,
            null,
            null,
            ContractStatus.Active,
            null,
            now));
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();
        var loadedProperty = await dbContext.Properties.SingleAsync(item => item.Id == property.Id);
        dbContext.Properties.Remove(loadedProperty);

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        Assert.Equal(1, await dbContext.TenantContracts.CountAsync());
    }

    [Fact]
    public async Task DevelopmentRealEstateSeeder_WhenRunTwice_IsIdempotent()
    {
        await using var connection = await OpenInMemorySqliteAsync();
        var options = CreateOptions(connection);

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var seeder = new DevelopmentRealEstateSeeder(dbContext, new FixedClock());

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Equal(4, await dbContext.Properties.CountAsync());
        Assert.Equal(1, await dbContext.LandlordContracts.CountAsync());
        Assert.Equal(1, await dbContext.TenantContracts.CountAsync());
        Assert.Equal(2, await dbContext.Leads.CountAsync());
        Assert.Equal(14, await dbContext.PropertyImages.CountAsync());
        Assert.All(
            await dbContext.Properties.Include(property => property.Images).ToListAsync(),
            property =>
            {
                Assert.True(property.Images.Count >= 3);
                Assert.All(property.Images, image =>
                {
                    Assert.StartsWith("/images/properties/demo/", image.Url);
                    Assert.EndsWith(".webp", image.Url);
                    Assert.NotEqual("/images/properties/property-placeholder.svg", image.Url);
                });
            });
        Assert.Contains(await dbContext.Properties.ToListAsync(), property => property.Status == PropertyStatus.Available && property.SalePrice is not null);
        Assert.Contains(await dbContext.Properties.ToListAsync(), property => property.Status == PropertyStatus.Occupied);
        Assert.Contains(await dbContext.Properties.ToListAsync(), property => property.Status == PropertyStatus.SoonAvailable);
        Assert.DoesNotContain(await dbContext.PropertyImages.ToListAsync(), image => image.Url.Contains("/images/fields/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(await dbContext.PropertyImages.ToListAsync(), image => image.Url.Contains("san-", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Migration_WhenAppliedToNewSqliteDatabase_CreatesLegacyAndRealEstateTables()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"real-estate-migration-{Guid.NewGuid():N}.db");

        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            await using (var dbContext = new ApplicationDbContext(options))
            {
                await dbContext.Database.MigrateAsync();

                var tables = await GetTableNamesAsync(dbContext);

                Assert.Contains("Fields", tables);
                Assert.Contains("Bookings", tables);
                Assert.Contains("Properties", tables);
                Assert.Contains("LandlordContracts", tables);
                Assert.Contains("TenantContracts", tables);
                Assert.Contains("Leads", tables);
                await dbContext.Database.CloseConnectionAsync();
            }
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions(SqliteConnection connection)
        => new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

    private static async Task<SqliteConnection> OpenInMemorySqliteAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys=ON;";
        await command.ExecuteNonQueryAsync();

        return connection;
    }

    private static async Task<IReadOnlyCollection<string>> GetTableNamesAsync(ApplicationDbContext dbContext)
    {
        var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames;
    }

    private static Property CreateProperty(string code, DateTimeOffset now)
        => new(
            Guid.NewGuid(),
            code,
            PropertyProject.OpusOne,
            "S1",
            PropertyType.TwoBedroomTwoBathrooms,
            68.5m,
            2,
            24_000_000,
            5_600_000_000,
            "Đông Nam",
            null,
            "Sổ hồng",
            "Nội thất cơ bản",
            "Căn hộ kiểm thử persistence.",
            null,
            PropertyStatus.Available,
            new DateOnly(2026, 8, 1),
            "Test",
            now);

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => new(2026, 7, 29, 0, 0, 0, TimeSpan.Zero);
    }
}

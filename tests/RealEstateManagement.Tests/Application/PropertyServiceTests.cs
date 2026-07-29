using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Application.Properties;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Tests.Application;

public sealed class PropertyServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListPropertiesAsync_WhenFiltered_ReturnsMatchingProperties()
    {
        var store = new InMemoryPropertyStore();
        store.Properties.Add(CreateProperty("OP-0101", PropertyProject.OpusOne, "S1", PropertyType.TwoBedroom, PropertyStatus.Available, 18_000_000, 5_000_000_000));
        store.Properties.Add(CreateProperty("OR-0202", PropertyProject.Origami, "S2", PropertyType.Studio, PropertyStatus.Occupied, 9_000_000, null));
        var service = new PropertyService(store, new FixedClock());

        var result = await service.ListPropertiesAsync(new PropertyFilterQuery(
            Keyword: "op",
            Project: PropertyProject.OpusOne,
            MinMonthlyPrice: 15_000_000,
            MaxMonthlyPrice: 20_000_000,
            MinSalePrice: 4_000_000_000,
            MaxSalePrice: 6_000_000_000,
            SalesOnly: true));

        var property = Assert.Single(result);
        Assert.Equal("OP-0101", property.Code);
    }

    [Fact]
    public async Task CreatePropertyAsync_WhenCodeDuplicate_ReturnsFailure()
    {
        var store = new InMemoryPropertyStore();
        store.Properties.Add(CreateProperty("OP-0101"));
        var service = new PropertyService(store, new FixedClock());

        var result = await service.CreatePropertyAsync(ValidCommand() with { Code = " op-0101 " });

        Assert.False(result.Succeeded);
        Assert.Contains("Mã căn hộ đã tồn tại.", result.Errors);
    }

    [Fact]
    public async Task CreatePropertyAsync_WhenValid_AddsProperty()
    {
        var store = new InMemoryPropertyStore();
        var service = new PropertyService(store, new FixedClock());

        var result = await service.CreatePropertyAsync(ValidCommand());

        Assert.True(result.Succeeded);
        var property = Assert.Single(store.Properties);
        Assert.Equal("OP-0101", property.Code);
        Assert.Equal(PropertyStatus.Available, property.Status);
    }

    [Fact]
    public async Task UpdatePropertyAsync_WhenValid_UpdatesDetails()
    {
        var store = new InMemoryPropertyStore();
        var property = CreateProperty("OP-0101");
        store.Properties.Add(property);
        var service = new PropertyService(store, new FixedClock());

        var result = await service.UpdatePropertyAsync(property.Id, ValidCommand() with { Code = "OP-0102", MonthlyPrice = 20_000_000 });

        Assert.True(result.Succeeded);
        Assert.Equal("OP-0102", property.Code);
        Assert.Equal(20_000_000, property.MonthlyPrice);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenValid_UpdatesPropertyStatus()
    {
        var store = new InMemoryPropertyStore();
        var property = CreateProperty("OP-0101");
        store.Properties.Add(property);
        var service = new PropertyService(store, new FixedClock());

        var result = await service.ChangeStatusAsync(new PropertyStatusCommand(property.Id, PropertyStatus.Reserved));

        Assert.True(result.Succeeded);
        Assert.Equal(PropertyStatus.Reserved, property.Status);
    }

    private static PropertyEditorCommand ValidCommand()
        => new(
            "OP-0101",
            PropertyProject.OpusOne,
            "S1",
            PropertyType.TwoBedroom,
            68.5m,
            2,
            18_000_000,
            5_000_000_000,
            "East",
            null,
            "Pink book",
            "Full furniture",
            "River view",
            null,
            PropertyStatus.Available,
            new DateOnly(2026, 8, 1),
            null,
            [new PropertyImageCommand("/images/properties/op-0101.jpg", "OP-0101", 1, true)],
            [new PropertyFurnitureItemCommand("Sofa", 1, null)],
            ["Pool", "Gym"]);

    private static Property CreateProperty(
        string code,
        PropertyProject? project = PropertyProject.OpusOne,
        string area = "S1",
        PropertyType type = PropertyType.TwoBedroom,
        PropertyStatus status = PropertyStatus.Available,
        long? monthlyPrice = 18_000_000,
        long? salePrice = 5_000_000_000)
        => new(
            Guid.NewGuid(),
            code,
            project,
            area,
            type,
            68.5m,
            2,
            monthlyPrice,
            salePrice,
            null,
            null,
            null,
            null,
            null,
            null,
            status,
            null,
            null,
            Now);

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class InMemoryPropertyStore : IPropertyStore
    {
        public List<Property> Properties { get; } = [];

        public Task<IReadOnlyList<PropertySummaryDto>> ListPropertiesAsync(PropertyFilterQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<Property> properties = Properties;
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                properties = properties.Where(property => property.Code.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase)
                    || property.Area.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase));
            }

            if (query.Project is not null)
            {
                properties = properties.Where(property => property.Project == query.Project);
            }

            if (!string.IsNullOrWhiteSpace(query.Area))
            {
                properties = properties.Where(property => string.Equals(property.Area, query.Area, StringComparison.OrdinalIgnoreCase));
            }

            if (query.Type is not null)
            {
                properties = properties.Where(property => property.Type == query.Type);
            }

            if (query.Status is not null)
            {
                properties = properties.Where(property => property.Status == query.Status);
            }

            if (query.SalesOnly)
            {
                properties = properties.Where(property => property.SalePrice > 0);
            }

            if (query.MinMonthlyPrice is not null)
            {
                properties = properties.Where(property => property.MonthlyPrice is not null && property.MonthlyPrice.Value >= query.MinMonthlyPrice.Value);
            }

            if (query.MaxMonthlyPrice is not null)
            {
                properties = properties.Where(property => property.MonthlyPrice is not null && property.MonthlyPrice.Value <= query.MaxMonthlyPrice.Value);
            }

            if (query.MinSalePrice is not null)
            {
                properties = properties.Where(property => property.SalePrice is not null && property.SalePrice.Value >= query.MinSalePrice.Value);
            }

            if (query.MaxSalePrice is not null)
            {
                properties = properties.Where(property => property.SalePrice is not null && property.SalePrice.Value <= query.MaxSalePrice.Value);
            }

            return Task.FromResult<IReadOnlyList<PropertySummaryDto>>(properties.Select(ToSummary).ToArray());
        }

        public Task<PropertyDetailDto?> GetPropertyDetailAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<PropertyDetailDto?>(null);

        public Task<Property?> GetPropertyForUpdateAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Properties.FirstOrDefault(property => property.Id == id));

        public Task<bool> CodeExistsAsync(string normalizedCode, Guid? exceptPropertyId, CancellationToken cancellationToken)
            => Task.FromResult(Properties.Any(property => property.Id != exceptPropertyId && property.Code == normalizedCode));

        public Task AddPropertyAsync(Property property, CancellationToken cancellationToken)
        {
            Properties.Add(property);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static PropertySummaryDto ToSummary(Property property)
            => new(
                property.Id,
                property.Code,
                property.Project,
                property.Area,
                property.Type,
                property.AreaSize,
                property.Bathrooms,
                property.MonthlyPrice,
                property.SalePrice,
                property.Status,
                property.AvailableFromDate,
                null);
    }
}

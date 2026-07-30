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
    public async Task ListPublicRentalsAsync_ReturnsOnlyPublicRentalProperties()
    {
        var store = new InMemoryPropertyStore();
        store.Properties.Add(CreateProperty("OP-0101", status: PropertyStatus.Available, monthlyPrice: 18_000_000));
        store.Properties.Add(CreateProperty("OR-0202", status: PropertyStatus.SoonAvailable, monthlyPrice: 15_000_000));
        store.Properties.Add(CreateProperty("GH-0303", status: PropertyStatus.Reserved, monthlyPrice: 16_000_000));
        store.Properties.Add(CreateProperty("BV-0404", status: PropertyStatus.Occupied, monthlyPrice: 20_000_000));
        store.Properties.Add(CreateProperty("NO-0505", status: PropertyStatus.Available, monthlyPrice: null));
        var service = new PropertyService(store, new FixedClock());

        var result = await service.ListPublicRentalsAsync(new PublicPropertyFilterQuery());

        Assert.Equal(["GH-***03", "OP-***01", "OR-***02"], result.Select(property => property.MaskedCode));
    }

    [Fact]
    public async Task ListPublicSalesAsync_ReturnsOnlyPropertiesWithSalePrice()
    {
        var store = new InMemoryPropertyStore();
        store.Properties.Add(CreateProperty("OP-0101", salePrice: 5_000_000_000));
        store.Properties.Add(CreateProperty("OR-0202", salePrice: null));
        var service = new PropertyService(store, new FixedClock());

        var result = await service.ListPublicSalesAsync(new PublicPropertyFilterQuery());

        var property = Assert.Single(result);
        Assert.Equal("OP-***01", property.MaskedCode);
    }

    [Fact]
    public async Task ListPublicRentalsAsync_WhenFilteredByProject_ReturnsMatchingProject()
    {
        var store = new InMemoryPropertyStore();
        store.Properties.Add(CreateProperty("OP-0101", PropertyProject.OpusOne));
        store.Properties.Add(CreateProperty("OR-0202", PropertyProject.Origami));
        var service = new PropertyService(store, new FixedClock());

        var result = await service.ListPublicRentalsAsync(new PublicPropertyFilterQuery(Project: PropertyProject.Origami));

        var property = Assert.Single(result);
        Assert.Equal(PropertyProject.Origami, property.Project);
    }

    [Fact]
    public async Task ListPublicRentalsAsync_WhenFilteredByArea_ReturnsMatchingArea()
    {
        var store = new InMemoryPropertyStore();
        store.Properties.Add(CreateProperty("OP-0101", area: "S1"));
        store.Properties.Add(CreateProperty("OR-0202", area: "S2"));
        var service = new PropertyService(store, new FixedClock());

        var result = await service.ListPublicRentalsAsync(new PublicPropertyFilterQuery(Area: "s2"));

        var property = Assert.Single(result);
        Assert.Equal("S2", property.Area);
    }

    [Fact]
    public async Task ListPublicRentalsAsync_WhenFilteredByType_ReturnsMatchingType()
    {
        var store = new InMemoryPropertyStore();
        store.Properties.Add(CreateProperty("OP-0101", type: PropertyType.TwoBedroom));
        store.Properties.Add(CreateProperty("OR-0202", type: PropertyType.Studio));
        var service = new PropertyService(store, new FixedClock());

        var result = await service.ListPublicRentalsAsync(new PublicPropertyFilterQuery(Type: PropertyType.Studio));

        var property = Assert.Single(result);
        Assert.Equal(PropertyType.Studio, property.Type);
    }

    [Fact]
    public async Task ListPublicRentalsAsync_WhenFilteredByStatus_ReturnsMatchingStatus()
    {
        var store = new InMemoryPropertyStore();
        store.Properties.Add(CreateProperty("OP-0101", status: PropertyStatus.Available));
        store.Properties.Add(CreateProperty("OR-0202", status: PropertyStatus.SoonAvailable));
        var service = new PropertyService(store, new FixedClock());

        var result = await service.ListPublicRentalsAsync(new PublicPropertyFilterQuery(Status: PropertyStatus.SoonAvailable));

        var property = Assert.Single(result);
        Assert.Equal(PropertyStatus.SoonAvailable, property.Status);
    }

    [Fact]
    public async Task ListPublicRentalsAsync_WhenFilteredByRentRange_ReturnsMatchingPrices()
    {
        var store = new InMemoryPropertyStore();
        store.Properties.Add(CreateProperty("OP-0101", monthlyPrice: 12_000_000));
        store.Properties.Add(CreateProperty("OR-0202", monthlyPrice: 18_000_000));
        store.Properties.Add(CreateProperty("GH-0303", monthlyPrice: 25_000_000));
        var service = new PropertyService(store, new FixedClock());

        var result = await service.ListPublicRentalsAsync(new PublicPropertyFilterQuery(MinPrice: 15_000_000, MaxPrice: 20_000_000));

        var property = Assert.Single(result);
        Assert.Equal(18_000_000, property.MonthlyPrice);
    }

    [Fact]
    public async Task ListPublicSalesAsync_WhenFilteredBySaleRange_ReturnsMatchingPrices()
    {
        var store = new InMemoryPropertyStore();
        store.Properties.Add(CreateProperty("OP-0101", salePrice: 4_000_000_000));
        store.Properties.Add(CreateProperty("OR-0202", salePrice: 5_500_000_000));
        store.Properties.Add(CreateProperty("GH-0303", salePrice: 7_000_000_000));
        var service = new PropertyService(store, new FixedClock());

        var result = await service.ListPublicSalesAsync(new PublicPropertyFilterQuery(MinPrice: 5_000_000_000, MaxPrice: 6_000_000_000));

        var property = Assert.Single(result);
        Assert.Equal(5_500_000_000, property.SalePrice);
    }

    [Fact]
    public async Task ListPublicSalesAsync_WhenSortedByPrice_ReturnsExpectedOrder()
    {
        var store = new InMemoryPropertyStore();
        store.Properties.Add(CreateProperty("OP-0101", salePrice: 6_000_000_000));
        store.Properties.Add(CreateProperty("OR-0202", salePrice: 4_500_000_000));
        var service = new PropertyService(store, new FixedClock());

        var result = await service.ListPublicSalesAsync(new PublicPropertyFilterQuery(SortBy: PublicPropertySortOptions.PriceAsc));

        Assert.Equal(["OR-***02", "OP-***01"], result.Select(property => property.MaskedCode));
    }

    [Fact]
    public async Task PublicPropertyDtos_MaskCodeAndDoNotExposeAdminOnlyFields()
    {
        var store = new InMemoryPropertyStore();
        var property = CreateProperty("OP-0101", salePrice: 5_000_000_000);
        store.Properties.Add(property);
        var service = new PropertyService(store, new FixedClock());

        var result = await service.GetPublicSaleDetailAsync(property.Id);

        Assert.NotNull(result);
        Assert.Equal("OP-***01", result.MaskedCode);
        var publicFields = typeof(PublicPropertyDetailDto).GetProperties().Select(info => info.Name).ToHashSet();
        Assert.DoesNotContain("Notes", publicFields);
        Assert.DoesNotContain("InputPrice", publicFields);
        Assert.DoesNotContain("LandlordName", publicFields);
        Assert.DoesNotContain("TenantName", publicFields);
        Assert.DoesNotContain("PassCode", publicFields);
    }

    [Fact]
    public async Task GetPublicRentalDetailAsync_WhenMissing_ReturnsNull()
    {
        var service = new PropertyService(new InMemoryPropertyStore(), new FixedClock());

        var result = await service.GetPublicRentalDetailAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicSaleDetailAsync_WhenPropertyHasNoSalePrice_ReturnsNull()
    {
        var store = new InMemoryPropertyStore();
        var property = CreateProperty("OP-0101", salePrice: null);
        store.Properties.Add(property);
        var service = new PropertyService(store, new FixedClock());

        var result = await service.GetPublicSaleDetailAsync(property.Id);

        Assert.Null(result);
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
            "Đông Nam",
            "Hỗ trợ vay theo chính sách ngân hàng.",
            "Sổ hồng",
            "Nội thất cơ bản",
            "Căn hộ sáng, view nội khu.",
            "https://example.com/video.mp4",
            status,
            new DateOnly(2026, 8, 1),
            "Ghi chú nội bộ không được public.",
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
            => Task.FromResult(Properties.FirstOrDefault(property => property.Id == id) is { } property ? ToDetail(property) : null);

        public Task<Property?> GetPropertyForUpdateAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Properties.FirstOrDefault(property => property.Id == id));

        public Task<bool> CodeExistsAsync(string normalizedCode, Guid? exceptPropertyId, CancellationToken cancellationToken)
            => Task.FromResult(Properties.Any(property => property.Id != exceptPropertyId && property.Code == normalizedCode));

        public Task<bool> HasContractRelationshipsAsync(Guid propertyId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task ClearPropertyChildrenAsync(Guid propertyId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task AddPropertyAsync(Property property, CancellationToken cancellationToken)
        {
            Properties.Add(property);
            return Task.CompletedTask;
        }

        public void DeleteProperty(Property property)
            => Properties.Remove(property);

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
                property.Images.OrderBy(image => image.SortOrder).FirstOrDefault(image => image.IsPrimary)?.Url,
                property.CreatedAtUtc);

        private static PropertyDetailDto ToDetail(Property property)
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
                property.Direction,
                property.LoanInfo,
                property.LegalStatus,
                property.FurniturePackage,
                property.Description,
                property.VideoUrl,
                property.Status,
                property.AvailableFromDate,
                property.Notes,
                property.CreatedAtUtc,
                property.UpdatedAtUtc,
                property.Images.Select(image => new PropertyImageDto(image.Id, image.Url, image.AltText, image.SortOrder, image.IsPrimary)).ToArray(),
                property.FurnitureItems.Select(item => new PropertyFurnitureItemDto(item.Id, item.Name, item.Quantity, item.Notes)).ToArray(),
                property.Amenities.Select(amenity => new PropertyAmenityDto(amenity.Id, amenity.Name)).ToArray());
    }
}

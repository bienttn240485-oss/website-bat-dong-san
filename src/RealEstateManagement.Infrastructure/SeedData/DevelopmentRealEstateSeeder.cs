using Microsoft.EntityFrameworkCore;
using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Domain.Properties;
using RealEstateManagement.Infrastructure.Data;

namespace RealEstateManagement.Infrastructure.SeedData;

public sealed class DevelopmentRealEstateSeeder(ApplicationDbContext dbContext, ISystemClock clock)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var properties = BuildProperties(now);

        foreach (var property in properties)
        {
            if (await dbContext.Properties.AnyAsync(existing => existing.Code == property.Code, cancellationToken))
            {
                continue;
            }

            await dbContext.Properties.AddAsync(property, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
        await EnsureSeedPropertyImagesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
        await EnsureDevelopmentPlaceholderImagesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        var opus = await dbContext.Properties.FirstOrDefaultAsync(property => property.Code == "OP-0101", cancellationToken);
        var origami = await dbContext.Properties.FirstOrDefaultAsync(property => property.Code == "ORI-1808", cancellationToken);

        if (opus is not null && !await dbContext.LandlordContracts.AnyAsync(contract => contract.PropertyId == opus.Id, cancellationToken))
        {
            await dbContext.LandlordContracts.AddAsync(new LandlordContract(
                Guid.NewGuid(),
                opus.Id,
                "Nguyễn Minh An",
                "PE-OP-0101",
                "Trần Khánh Linh",
                18_000_000,
                new DateOnly(2026, 7, 1),
                null,
                DepositStatus.Pending,
                5,
                "Ngày 1-5 hằng tháng",
                new DateOnly(2026, 8, 5),
                "Hợp đồng quản lý căn mẫu phát triển.",
                now), cancellationToken);
        }

        if (opus is not null && !await dbContext.TenantContracts.AnyAsync(contract => contract.PropertyId == opus.Id, cancellationToken))
        {
            await dbContext.TenantContracts.AddAsync(new TenantContract(
                Guid.NewGuid(),
                opus.Id,
                "Lê Hoàng Nam",
                "Trần Khánh Linh",
                24_000_000,
                new DateOnly(2026, 7, 15),
                12,
                48_000_000,
                null,
                "PE-OP-0101",
                "2407",
                ContractStatus.Active,
                "Khách thuê dài hạn, thanh toán theo tháng.",
                now), cancellationToken);
        }

        if (!await dbContext.Leads.AnyAsync(lead => lead.Contact == "0901 111 222", cancellationToken))
        {
            await dbContext.Leads.AddAsync(new Lead(
                Guid.NewGuid(),
                "Phạm Thu Hà",
                "0901 111 222",
                origami?.Id,
                "Tư vấn căn bán",
                "Khách quan tâm căn hai phòng ngủ tại Origami.",
                "vi",
                now), cancellationToken);
        }

        if (!await dbContext.Leads.AnyAsync(lead => lead.Contact == "0902 333 444", cancellationToken))
        {
            await dbContext.Leads.AddAsync(new Lead(
                Guid.NewGuid(),
                "Đặng Quốc Huy",
                "0902 333 444",
                null,
                "Tìm căn thuê",
                "Khách cần căn trống trong tháng tới, ngân sách dưới 20 triệu.",
                "vi",
                now), cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private static IReadOnlyList<Property> BuildProperties(DateTimeOffset now)
        => [
            CreateProperty(
                "OP-0101",
                PropertyProject.OpusOne,
                "S1",
                PropertyType.TwoBedroomTwoBathrooms,
                PropertyStatus.Occupied,
                68.5m,
                2,
                24_000_000,
                5_600_000_000,
                "Đông Nam",
                "Sổ hồng, hỗ trợ vay ngân hàng",
                "Nội thất đầy đủ",
                "Căn góc tầng trung, view sông và công viên.",
                new DateOnly(2027, 7, 15),
                now),
            CreateProperty(
                "ORI-1808",
                PropertyProject.Origami,
                "Origami S8",
                PropertyType.TwoBedroomOneBathroom,
                PropertyStatus.Available,
                59.2m,
                1,
                17_000_000,
                4_200_000_000,
                "Tây Bắc",
                "Pháp lý đầy đủ",
                "Nội thất cơ bản",
                "Căn sáng, phù hợp gia đình nhỏ hoặc chuyên gia làm việc tại TP. Thủ Đức.",
                new DateOnly(2026, 8, 1),
                now),
            CreateProperty(
                "GH-2312",
                PropertyProject.GloryHeights,
                "Glory Heights",
                PropertyType.OneBedroomPlus,
                PropertyStatus.SoonAvailable,
                51.0m,
                1,
                15_000_000,
                null,
                "Đông",
                "Hợp đồng thuê sắp kết thúc",
                "Nội thất đẹp",
                "Căn một phòng ngủ cộng, sắp trống trong 30 ngày.",
                new DateOnly(2026, 8, 20),
                now),
            CreateProperty(
                "BEV-0906",
                PropertyProject.Beverly,
                "Beverly",
                PropertyType.ThreeBedroomTwoBathrooms,
                PropertyStatus.Available,
                92.0m,
                2,
                32_000_000,
                8_900_000_000,
                "Nam",
                "Sổ hồng",
                "Nội thất cao cấp",
                "Căn lớn cho gia đình, có ban công rộng và nhiều ánh sáng.",
                null,
                now)
        ];

    private static Property CreateProperty(
        string code,
        PropertyProject project,
        string area,
        PropertyType type,
        PropertyStatus status,
        decimal areaSize,
        int bathrooms,
        long monthlyPrice,
        long? salePrice,
        string direction,
        string legalStatus,
        string furniturePackage,
        string description,
        DateOnly? availableFromDate,
        DateTimeOffset now)
    {
        var propertyId = Guid.NewGuid();
        var property = new Property(
            propertyId,
            code,
            project,
            area,
            type,
            areaSize,
            bathrooms,
            monthlyPrice,
            salePrice,
            direction,
            null,
            legalStatus,
            furniturePackage,
            description,
            null,
            status,
            availableFromDate,
            "Dữ liệu mẫu phát triển.",
            now);

        property.ReplaceImages(BuildImages(propertyId, code));
        property.ReplaceFurnitureItems([
            new PropertyFurnitureItem(Guid.NewGuid(), propertyId, "Sofa", 1, null),
            new PropertyFurnitureItem(Guid.NewGuid(), propertyId, "Giường", type is PropertyType.Studio ? 1 : 2, null)
        ]);
        property.ReplaceAmenities([
            new PropertyAmenity(Guid.NewGuid(), propertyId, "Hồ bơi"),
            new PropertyAmenity(Guid.NewGuid(), propertyId, "Phòng gym")
        ]);

        return property;
    }

    private async Task EnsureSeedPropertyImagesAsync(CancellationToken cancellationToken)
    {
        foreach (var code in SeedImageUrlsByCode.Keys)
        {
            var property = await dbContext.Properties
                .AsNoTracking()
                .Where(item => item.Code == code)
                .Select(item => new { item.Id, item.Code })
                .FirstOrDefaultAsync(cancellationToken);
            if (property is null)
            {
                continue;
            }

            var existingImages = await dbContext.PropertyImages
                .Where(image => image.PropertyId == property.Id)
                .OrderBy(image => image.SortOrder)
                .ToListAsync(cancellationToken);
            var desiredUrls = ResolveDemoImageUrls(property.Code);

            if (existingImages.Select(image => image.Url).SequenceEqual(desiredUrls, StringComparer.Ordinal))
            {
                continue;
            }

            if (existingImages.Count > 0 && existingImages.Any(image => !IsReplaceableDevelopmentImage(image.Url, property.Code)))
            {
                continue;
            }

            dbContext.PropertyImages.RemoveRange(existingImages);
            await dbContext.PropertyImages.AddRangeAsync(BuildImages(property.Id, property.Code), cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private async Task EnsureDevelopmentPlaceholderImagesAsync(CancellationToken cancellationToken)
    {
        var properties = await dbContext.Properties
            .AsNoTracking()
            .Select(item => new { item.Id, item.Code })
            .ToListAsync(cancellationToken);

        foreach (var property in properties)
        {
            if (SeedImageUrlsByCode.ContainsKey(property.Code))
            {
                continue;
            }

            var existingImages = await dbContext.PropertyImages
                .Where(image => image.PropertyId == property.Id)
                .OrderBy(image => image.SortOrder)
                .ToListAsync(cancellationToken);

            if (existingImages.Count >= 3 && existingImages.All(image => IsRealEstateDemoImage(image.Url)))
            {
                continue;
            }

            if (existingImages.Count > 0 && existingImages.Any(image => !IsReplaceableDevelopmentImage(image.Url, property.Code)))
            {
                continue;
            }

            dbContext.PropertyImages.RemoveRange(existingImages);
            await dbContext.PropertyImages.AddRangeAsync(BuildImages(property.Id, property.Code), cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<PropertyImage> BuildImages(Guid propertyId, string code)
        => ResolveDemoImageUrls(code)
            .Select((url, index) => new PropertyImage(
                Guid.NewGuid(),
                propertyId,
                url,
                BuildImageAltText(code, index),
                index + 1,
                index == 0)).ToArray();

    private static string BuildImageAltText(string code, int index)
        => index switch
        {
            0 => $"Phòng khách căn hộ {code}",
            1 => $"Phòng ngủ căn hộ {code}",
            2 => $"Bếp căn hộ {code}",
            3 => $"Ban công căn hộ {code}",
            _ => $"Tiện ích căn hộ {code}"
        };

    private static bool IsReplaceableDevelopmentImage(string url, string code)
        => string.Equals(url, $"/images/properties/{code.ToLowerInvariant()}.jpg", StringComparison.Ordinal)
            || string.Equals(url, "/images/properties/test.jpg", StringComparison.Ordinal)
            || string.Equals(url, PublicFallbackImage, StringComparison.Ordinal)
            || IsOldAbstractDemoImage(url)
            || IsRealEstateDemoImage(url);

    private static bool IsOldAbstractDemoImage(string url)
        => url is "/images/properties/apartment-living-room-01.svg"
            or "/images/properties/apartment-bedroom-01.svg"
            or "/images/properties/apartment-kitchen-01.svg"
            or "/images/properties/apartment-balcony-01.svg"
            or "/images/properties/apartment-amenity-01.svg";

    private static bool IsRealEstateDemoImage(string url)
        => url.StartsWith("/images/properties/demo/", StringComparison.Ordinal)
            && url.EndsWith(".webp", StringComparison.Ordinal);

    private static string[] ResolveDemoImageUrls(string code)
        => SeedImageUrlsByCode.TryGetValue(code, out var urls)
            ? urls
            : DemoImageSets[(int)((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(code) % DemoImageSets.Length)];

    private const string PublicFallbackImage = "/images/properties/property-placeholder.svg";

    private static readonly IReadOnlyDictionary<string, string[]> SeedImageUrlsByCode = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["OP-0101"] =
        [
            "/images/properties/demo/living-room-01.webp",
            "/images/properties/demo/bedroom-01.webp",
            "/images/properties/demo/kitchen-01.webp",
            "/images/properties/demo/balcony-01.webp"
        ],
        ["ORI-1808"] =
        [
            "/images/properties/demo/living-room-02.webp",
            "/images/properties/demo/bedroom-02.webp",
            "/images/properties/demo/kitchen-02.webp"
        ],
        ["GH-2312"] =
        [
            "/images/properties/demo/living-room-03.webp",
            "/images/properties/demo/bedroom-03.webp",
            "/images/properties/demo/apartment-exterior-01.webp"
        ],
        ["BEV-0906"] =
        [
            "/images/properties/demo/balcony-01.webp",
            "/images/properties/demo/living-room-01.webp",
            "/images/properties/demo/bedroom-02.webp",
            "/images/properties/demo/kitchen-02.webp"
        ]
    };

    private static readonly string[][] DemoImageSets =
    [
        [
            "/images/properties/demo/living-room-01.webp",
            "/images/properties/demo/bedroom-01.webp",
            "/images/properties/demo/kitchen-01.webp"
        ],
        [
            "/images/properties/demo/living-room-02.webp",
            "/images/properties/demo/bedroom-02.webp",
            "/images/properties/demo/balcony-01.webp"
        ],
        [
            "/images/properties/demo/living-room-03.webp",
            "/images/properties/demo/bedroom-03.webp",
            "/images/properties/demo/kitchen-02.webp"
        ],
        [
            "/images/properties/demo/apartment-exterior-01.webp",
            "/images/properties/demo/living-room-01.webp",
            "/images/properties/demo/bedroom-02.webp",
            "/images/properties/demo/balcony-01.webp"
        ]
    ];
}

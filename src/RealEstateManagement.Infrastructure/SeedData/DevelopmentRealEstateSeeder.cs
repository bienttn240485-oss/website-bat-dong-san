using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Domain.Properties;
using RealEstateManagement.Infrastructure.Data;

namespace RealEstateManagement.Infrastructure.SeedData;

public sealed class DevelopmentRealEstateSeeder(ApplicationDbContext dbContext)
{
    private static readonly string[] DemoImages =
    [
        "/images/properties/demo/living-room-01.webp",
        "/images/properties/demo/living-room-02.webp",
        "/images/properties/demo/living-room-03.webp",
        "/images/properties/demo/bedroom-01.webp",
        "/images/properties/demo/bedroom-02.webp",
        "/images/properties/demo/bedroom-03.webp",
        "/images/properties/demo/kitchen-01.webp",
        "/images/properties/demo/kitchen-02.webp",
        "/images/properties/demo/balcony-01.webp",
        "/images/properties/demo/apartment-exterior-01.webp"
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPropertiesAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        var seedCodes = PropertySeeds.Select(seed => seed.Code).ToArray();
        var propertyIds = await dbContext.Properties
            .AsNoTracking()
            .Where(property => seedCodes.Contains(property.Code))
            .ToDictionaryAsync(property => property.Code, property => property.Id, cancellationToken);
        var saleUsers = await dbContext.Users
            .AsNoTracking()
            .Where(user => SaleEmails.Contains(user.Email!))
            .Select(user => new { user.Email, user.Id })
            .ToListAsync(cancellationToken);
        var saleUserIds = saleUsers.ToDictionary(user => user.Email!, user => user.Id, StringComparer.OrdinalIgnoreCase);

        await SeedLandlordContractsAsync(propertyIds, cancellationToken);
        await SeedTenantContractsAsync(propertyIds, cancellationToken);
        await SeedLeadsAsync(propertyIds, saleUserIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private async Task SeedPropertiesAsync(CancellationToken cancellationToken)
    {
        foreach (var seed in PropertySeeds)
        {
            if (await dbContext.Properties.AnyAsync(property => property.Code == seed.Code, cancellationToken))
            {
                continue;
            }

            var property = new Property(
                DeterministicGuid($"property:{seed.Code}"),
                seed.Code,
                seed.Project,
                seed.Area,
                seed.Type,
                seed.AreaSize,
                seed.Bathrooms,
                seed.MonthlyPrice,
                seed.SalePrice,
                seed.Direction,
                seed.LoanInfo,
                seed.LegalStatus,
                seed.FurniturePackage,
                seed.Description,
                seed.VideoUrl,
                seed.Status,
                seed.AvailableFromDate,
                "Dữ liệu mẫu development.",
                seed.CreatedAtUtc);

            property.ReplaceImages(BuildImages(property.Id, seed.Code, seed.ImageOffset));
            property.ReplaceFurnitureItems(BuildFurniture(property.Id, seed.Type, seed.ImageOffset));
            property.ReplaceAmenities(BuildAmenities(property.Id, seed.ImageOffset));
            await dbContext.Properties.AddAsync(property, cancellationToken);
        }
    }

    private async Task SeedLandlordContractsAsync(IReadOnlyDictionary<string, Guid> propertyIds, CancellationToken cancellationToken)
    {
        foreach (var seed in PropertySeeds.Where(seed => !MissingLandlordCodes.Contains(seed.Code)))
        {
            if (!propertyIds.TryGetValue(seed.Code, out var propertyId)
                || await dbContext.LandlordContracts.AnyAsync(contract => contract.PropertyId == propertyId, cancellationToken))
            {
                continue;
            }

            await dbContext.LandlordContracts.AddAsync(new LandlordContract(
                DeterministicGuid($"landlord:{seed.Code}"),
                propertyId,
                LandlordName(seed.ImageOffset),
                $"PE-{seed.Code}",
                SaleName(seed.ImageOffset),
                InputPrice(seed),
                SignedDate(seed.ImageOffset),
                LandlordExpiryDate(seed.ImageOffset),
                DepositStatusValue(seed.ImageOffset),
                (seed.ImageOffset % 28) + 1,
                $"Ngày {(seed.ImageOffset % 28) + 1}-{Math.Min((seed.ImageOffset % 28) + 5, 31)} hằng tháng",
                new DateOnly(2026, 8, ((seed.ImageOffset % 24) + 1)),
                "Hợp đồng chủ nhà mẫu development.",
                seed.CreatedAtUtc.AddHours(1)), cancellationToken);
        }
    }

    private async Task SeedTenantContractsAsync(IReadOnlyDictionary<string, Guid> propertyIds, CancellationToken cancellationToken)
    {
        var tenantIndex = 0;
        foreach (var seed in PropertySeeds)
        {
            if (!propertyIds.TryGetValue(seed.Code, out var propertyId))
            {
                continue;
            }

            if (seed.Status is PropertyStatus.Occupied or PropertyStatus.SoonAvailable)
            {
                var signedDate = seed.Status == PropertyStatus.SoonAvailable
                    ? new DateOnly(2026, 5, 10 + tenantIndex % 10)
                    : new DateOnly(2026, 7, 20 + tenantIndex % 8);
                var termMonths = seed.Status == PropertyStatus.SoonAvailable ? 3 : TenantTerms[tenantIndex % TenantTerms.Length];

                await AddTenantContractAsync(
                    propertyId,
                    seed,
                    TenantName(tenantIndex),
                    signedDate,
                    termMonths,
                    ContractStatus.Active,
                    null,
                    tenantIndex,
                    cancellationToken);
                tenantIndex++;
            }

            if (tenantIndex % 3 == 0)
            {
                await AddTenantContractAsync(
                    propertyId,
                    seed,
                    $"Khách cũ {tenantIndex:00}",
                    new DateOnly(2025, 7, 20),
                    12,
                    tenantIndex % 2 == 0 ? ContractStatus.Expired : ContractStatus.Renewed,
                    new DateOnly(2026, 7, 22 + tenantIndex % 5),
                    tenantIndex + 40,
                    cancellationToken);
            }
            else if (tenantIndex % 5 == 0)
            {
                await AddTenantContractAsync(
                    propertyId,
                    seed,
                    $"Khách hủy {tenantIndex:00}",
                    new DateOnly(2026, 6, 1),
                    6,
                    ContractStatus.Cancelled,
                    null,
                    tenantIndex + 60,
                    cancellationToken);
            }
        }
    }

    private async Task AddTenantContractAsync(
        Guid propertyId,
        PropertySeed seed,
        string tenantName,
        DateOnly signedDate,
        int termMonths,
        ContractStatus status,
        DateOnly? depositReturnDate,
        int index,
        CancellationToken cancellationToken)
    {
        if (await dbContext.TenantContracts.AnyAsync(contract => contract.PropertyId == propertyId && contract.TenantName == tenantName, cancellationToken))
        {
            return;
        }

        var rentalPrice = seed.MonthlyPrice;
        await dbContext.TenantContracts.AddAsync(new TenantContract(
            DeterministicGuid($"tenant:{seed.Code}:{tenantName}"),
            propertyId,
            tenantName,
            SaleName(index),
            rentalPrice,
            signedDate,
            termMonths,
            rentalPrice * 2,
            depositReturnDate,
            $"PE-{seed.Code}",
            $"{1000 + index}",
            status,
            "Hợp đồng khách thuê mẫu development.",
            DevelopmentTimeline.AtUtc(2026, 7, 24 + index % 6, 3)), cancellationToken);
    }

    private async Task SeedLeadsAsync(
        IReadOnlyDictionary<string, Guid> propertyIds,
        IReadOnlyDictionary<string, Guid> saleUserIds,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < LeadNames.Length; index++)
        {
            var contact = $"090{index / 10}{index % 10} {200 + index:000} {300 + index:000}";
            if (await dbContext.Leads.AnyAsync(lead => lead.Contact == contact, cancellationToken))
            {
                continue;
            }

            var code = index % 4 == 0 ? null : PropertySeeds[index % PropertySeeds.Length].Code;
            var createdAt = DevelopmentTimeline.AtUtc(2026, 7, 24 + index % 7, 2 + index % 8);
            var lead = new Lead(
                DeterministicGuid($"lead:{contact}"),
                LeadNames[index],
                contact,
                code is not null && propertyIds.TryGetValue(code, out var propertyId) ? propertyId : null,
                index % 4 == 0 ? "Liên hệ tư vấn" : "Tư vấn căn hộ",
                LeadMessages[index % LeadMessages.Length],
                LeadLanguages[index % LeadLanguages.Length],
                createdAt);

            var status = LeadStatuses[index % LeadStatuses.Length];
            if (status != LeadStatus.New)
            {
                lead.ChangeStatus(status, createdAt.AddHours(8));
            }

            if (index % 6 != 0 && saleUserIds.TryGetValue(SaleEmails[index % SaleEmails.Length], out var saleUserId))
            {
                lead.AssignTo(saleUserId, createdAt.AddHours(10));
            }

            await dbContext.Leads.AddAsync(lead, cancellationToken);
        }
    }

    private static IReadOnlyList<PropertyImage> BuildImages(Guid propertyId, string code, int offset)
        => Enumerable.Range(0, 4)
            .Select(index =>
            {
                var url = DemoImages[(offset + index) % DemoImages.Length];
                return new PropertyImage(
                    DeterministicGuid($"image:{code}:{index}"),
                    propertyId,
                    url,
                    ImageAltText(code, index),
                    index + 1,
                    index == 0);
            })
            .ToArray();

    private static IReadOnlyList<PropertyFurnitureItem> BuildFurniture(Guid propertyId, PropertyType type, int offset)
    {
        var beds = type switch
        {
            PropertyType.Studio => 1,
            PropertyType.OneBedroom or PropertyType.OneBedroomPlus => 1,
            PropertyType.TwoBedroom or PropertyType.TwoBedroomPlus or PropertyType.TwoBedroomOneBathroom or PropertyType.TwoBedroomTwoBathrooms => 2,
            _ => 3
        };

        return
        [
            new PropertyFurnitureItem(DeterministicGuid($"furniture:{propertyId}:sofa"), propertyId, "Sofa", 1, null),
            new PropertyFurnitureItem(DeterministicGuid($"furniture:{propertyId}:bed"), propertyId, "Giường", beds, null),
            new PropertyFurnitureItem(DeterministicGuid($"furniture:{propertyId}:washer"), propertyId, "Máy giặt", 1, null)
        ];
    }

    private static IReadOnlyList<PropertyAmenity> BuildAmenities(Guid propertyId, int offset)
        =>
        [
            new PropertyAmenity(DeterministicGuid($"amenity:{propertyId}:pool"), propertyId, AmenityPool[offset % AmenityPool.Length]),
            new PropertyAmenity(DeterministicGuid($"amenity:{propertyId}:gym"), propertyId, AmenityPool[(offset + 1) % AmenityPool.Length]),
            new PropertyAmenity(DeterministicGuid($"amenity:{propertyId}:park"), propertyId, AmenityPool[(offset + 2) % AmenityPool.Length])
        ];

    private static long InputPrice(PropertySeed seed)
        => seed.Code == "GH-B1-3301"
            ? seed.MonthlyPrice + 2_000_000
            : Math.Max(seed.MonthlyPrice - 3_000_000, 6_000_000);

    private static DateOnly SignedDate(int index)
        => new(2026, 7, 20 + index % 10);

    private static DateOnly LandlordExpiryDate(int index)
        => index % 11 == 0 ? new DateOnly(2026, 8, 20 + index % 5) : new DateOnly(2027, 7, 20 + index % 10);

    private static string ImageAltText(string code, int index)
        => index switch
        {
            0 => $"Phòng khách căn hộ {code}",
            1 => $"Phòng ngủ căn hộ {code}",
            2 => $"Bếp căn hộ {code}",
            _ => $"Ban công căn hộ {code}"
        };

    private static DepositStatus DepositStatusValue(int index)
        => (DepositStatus)(index % 4);

    private static string LandlordName(int index)
        => Landlords[index % Landlords.Length];

    private static string TenantName(int index)
        => Tenants[index % Tenants.Length];

    private static string SaleName(int index)
        => Sales[index % Sales.Length];

    private static Guid DeterministicGuid(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"RealEstateManagement:Development:{value}"));
        return new Guid(bytes);
    }

    private sealed record PropertySeed(
        string Code,
        PropertyProject Project,
        string Area,
        PropertyType Type,
        PropertyStatus Status,
        decimal AreaSize,
        int Bathrooms,
        long MonthlyPrice,
        long? SalePrice,
        string Direction,
        string LegalStatus,
        string FurniturePackage,
        string Description,
        DateOnly? AvailableFromDate,
        DateTimeOffset CreatedAtUtc,
        int ImageOffset,
        string? LoanInfo = null,
        string? VideoUrl = null);

    private static readonly string[] SaleEmails =
    [
        "sale.tham@anphurealestate.local",
        "sale.thuy@anphurealestate.local",
        "sale.tuan@anphurealestate.local",
        "sale.linh@anphurealestate.local",
        "sale.huy@anphurealestate.local"
    ];

    private static readonly HashSet<string> MissingLandlordCodes = ["OR-S10-2508", "BV-C-1010", "BS9-3105", "LBV-D-3401", "MAN-C-3007", "OP-D-1505"];
    private static readonly int[] TenantTerms = [3, 6, 12];
    private static readonly string[] AmenityPool = ["Hồ bơi", "Phòng gym", "Công viên", "Khu BBQ", "Sảnh lễ tân", "Khu trẻ em"];
    private static readonly string[] Landlords = ["Nguyễn Minh An", "Trần Ngọc Mai", "Lê Hoàng Quân", "Phạm Thùy Dung", "Đỗ Thanh Bình"];
    private static readonly string[] Tenants = ["Lê Hoàng Nam", "Đặng Thu Hà", "Nguyễn Quốc Việt", "Trần Gia Hân", "Phạm Khánh Linh", "Hoàng Minh Khang"];
    private static readonly string[] Sales = ["Nguyễn Thị Thắm", "Trần Thu Thủy", "Nguyễn Minh Tuấn", "Lê Hoài Linh", "Phạm Quốc Huy"];
    private static readonly LeadStatus[] LeadStatuses = [LeadStatus.New, LeadStatus.Contacted, LeadStatus.Viewing, LeadStatus.Converted, LeadStatus.Lost];
    private static readonly string[] LeadLanguages = ["vi", "vi", "vi", "en", "zh"];
    private static readonly string[] LeadMessages = ["Muốn xem căn trong tuần này.", "Cần tư vấn căn có ban công.", "Quan tâm căn gần công viên.", "Cần căn phù hợp chuyên gia nước ngoài."];
    private static readonly string[] LeadNames =
    [
        "Nguyễn Phương Anh", "Trần Đức Minh", "Lê Thanh Mai", "Phạm Gia Bảo", "Hoàng Ngọc Hân",
        "Đỗ Quốc Khánh", "Vũ Minh Châu", "Bùi Anh Tuấn", "Ngô Thảo Vy", "Đặng Hà My",
        "Lý Gia Huy", "Chu Thu Trang", "Mai Nhật Nam", "Tạ Hoàng Yến", "Cao Minh Đức",
        "Nguyễn Hồng Nhung", "Trần Anh Khoa", "Lê Bảo Ngọc", "Phạm Tuấn Kiệt", "Hoàng Mỹ Linh",
        "Đỗ Phương Nam", "Vũ Khánh Chi", "Bùi Minh Tâm", "Ngô Gia Long", "Đặng Thanh Trúc",
        "Lý Tuệ Mẫn", "Chu Quang Huy", "Mai Linh Đan", "Tạ Gia Phúc", "Cao Ngọc Mai"
    ];

    private static readonly PropertySeed[] PropertySeeds =
    [
        new("TR-A1-0801", PropertyProject.TheRainbow, "The Rainbow", PropertyType.Studio, PropertyStatus.Available, 32.5m, 1, 8_000_000, 2_500_000_000, "Đông", "Sổ hồng", "Nội thất cơ bản", "Studio gọn gàng, phù hợp khách độc thân.", new DateOnly(2026, 7, 30), DevelopmentTimeline.AtUtc(2026, 7, 20), 0),
        new("TR-A2-1205", PropertyProject.TheRainbow, "The Rainbow", PropertyType.OneBedroom, PropertyStatus.Available, 45.0m, 1, 10_000_000, null, "Tây Bắc", "Hợp đồng mua bán", "Rèm, máy lạnh", "Căn một phòng ngủ sáng và thoáng.", new DateOnly(2026, 8, 1), DevelopmentTimeline.AtUtc(2026, 7, 20, 5), 1),
        new("TR-S3-2308", PropertyProject.TheRainbow, "The Rainbow", PropertyType.TwoBedroom, PropertyStatus.Reserved, 59.0m, 1, 12_000_000, 3_000_000_000, "Nam", "Sổ hồng", "Nội thất đầy đủ", "Căn đang giữ chỗ, có thể tư vấn phương án tương tự.", null, DevelopmentTimeline.AtUtc(2026, 7, 21), 2),
        new("OR-S7-1002", PropertyProject.Origami, "Origami S7", PropertyType.OneBedroomPlus, PropertyStatus.Available, 51.5m, 1, 12_000_000, null, "Đông Nam", "Sổ hồng", "Nội thất cơ bản", "Căn một phòng ngủ cộng tại Origami.", new DateOnly(2026, 7, 31), DevelopmentTimeline.AtUtc(2026, 7, 21, 4), 3),
        new("OR-S8-1805", PropertyProject.Origami, "Origami S8", PropertyType.TwoBedroomOneBathroom, PropertyStatus.Occupied, 59.2m, 1, 15_000_000, 3_500_000_000, "Tây", "Sổ hồng", "Nội thất đầy đủ", "Căn hai phòng ngủ đang có khách thuê ổn định.", null, DevelopmentTimeline.AtUtc(2026, 7, 22), 4),
        new("OR-S10-2508", PropertyProject.Origami, "Origami S10", PropertyType.TwoBedroomTwoBathrooms, PropertyStatus.SoonAvailable, 68.0m, 2, 18_000_000, 4_200_000_000, "Bắc", "Sổ hồng", "Nội thất đẹp", "Căn sắp trống, phù hợp gia đình nhỏ.", new DateOnly(2026, 8, 12), DevelopmentTimeline.AtUtc(2026, 7, 22, 5), 5, "Hỗ trợ vay 65%."),
        new("OR-S9-0712", PropertyProject.Origami, "Origami S9", PropertyType.Studio, PropertyStatus.Available, 31.8m, 1, 8_000_000, null, "Nam", "Hợp đồng mua bán", "Trống", "Studio giá tốt, dễ vào ở.", new DateOnly(2026, 7, 30), DevelopmentTimeline.AtUtc(2026, 7, 23), 6),
        new("GH-A1-1209", PropertyProject.GloryHeights, "Glory Heights A1", PropertyType.TwoBedroom, PropertyStatus.Occupied, 60.0m, 1, 18_000_000, null, "Đông Nam", "Sổ hồng", "Nội thất đầy đủ", "Căn view nội khu tại Glory Heights.", null, DevelopmentTimeline.AtUtc(2026, 7, 23, 4), 7),
        new("GH-A2-2106", PropertyProject.GloryHeights, "Glory Heights A2", PropertyType.TwoBedroomPlus, PropertyStatus.Available, 72.0m, 2, 20_000_000, 5_200_000_000, "Tây Nam", "Sổ hồng", "Nội thất cao cấp", "Căn hai phòng ngủ cộng, ban công rộng.", new DateOnly(2026, 8, 2), DevelopmentTimeline.AtUtc(2026, 7, 23, 6), 8),
        new("GH-B1-3301", PropertyProject.GloryHeights, "Glory Heights B1", PropertyType.ThreeBedroom, PropertyStatus.Occupied, 88.0m, 2, 25_000_000, 6_000_000_000, "Đông", "Sổ hồng", "Nội thất cao cấp", "Căn ba phòng ngủ có chênh lệch âm để kiểm tra cảnh báo.", null, DevelopmentTimeline.AtUtc(2026, 7, 24), 9),
        new("GH-C1-0908", PropertyProject.GloryHeights, "Glory Heights C1", PropertyType.OneBedroom, PropertyStatus.SoonAvailable, 46.0m, 1, 15_000_000, null, "Bắc", "Sổ hồng", "Nội thất cơ bản", "Căn sắp hết hợp đồng thuê.", new DateOnly(2026, 8, 18), DevelopmentTimeline.AtUtc(2026, 7, 24, 5), 1),
        new("BV-A-1407", PropertyProject.Beverly, "Beverly", PropertyType.ThreeBedroomTwoBathrooms, PropertyStatus.Available, 92.0m, 2, 30_000_000, 7_500_000_000, "Nam", "Sổ hồng", "Nội thất cao cấp", "Căn lớn cho gia đình.", new DateOnly(2026, 8, 1), DevelopmentTimeline.AtUtc(2026, 7, 24, 7), 2, "Hỗ trợ vay 70%."),
        new("BV-B-2603", PropertyProject.Beverly, "Beverly", PropertyType.TwoBedroomTwoBathrooms, PropertyStatus.Occupied, 70.0m, 2, 25_000_000, null, "Tây Bắc", "Sổ hồng", "Nội thất đầy đủ", "Căn đang vận hành cho thuê.", null, DevelopmentTimeline.AtUtc(2026, 7, 25), 3),
        new("BV-C-1010", PropertyProject.Beverly, "Beverly", PropertyType.ThreeBedroomPlus, PropertyStatus.Reserved, 105.0m, 2, 35_000_000, 9_000_000_000, "Đông Nam", "Sổ hồng", "Nội thất cao cấp", "Căn cao cấp đang giữ chỗ.", null, DevelopmentTimeline.AtUtc(2026, 7, 25, 4), 4),
        new("BS7-2508", PropertyProject.BeverlySolari, "Beverly Solari", PropertyType.TwoBedroomOneBathroom, PropertyStatus.SoonAvailable, 58.0m, 1, 18_000_000, 4_200_000_000, "Tây", "Sổ hồng", "Nội thất đẹp", "Căn Beverly Solari sắp trống.", new DateOnly(2026, 8, 15), DevelopmentTimeline.AtUtc(2026, 7, 25, 7), 5),
        new("BS7-1802", PropertyProject.BeverlySolari, "Beverly Solari", PropertyType.OneBedroomPlus, PropertyStatus.Available, 50.0m, 1, 15_000_000, null, "Đông", "Hợp đồng mua bán", "Rèm, máy lạnh", "Căn mới vào giỏ thuê.", new DateOnly(2026, 7, 30), DevelopmentTimeline.AtUtc(2026, 7, 26), 6),
        new("BS8-0906", PropertyProject.BeverlySolari, "Beverly Solari", PropertyType.TwoBedroomTwoBathrooms, PropertyStatus.Occupied, 68.0m, 2, 20_000_000, 5_200_000_000, "Nam", "Sổ hồng", "Nội thất đầy đủ", "Căn đang có hợp đồng thuê.", null, DevelopmentTimeline.AtUtc(2026, 7, 26, 3), 7),
        new("BS9-3105", PropertyProject.BeverlySolari, "Beverly Solari", PropertyType.ThreeBedroom, PropertyStatus.Reserved, 90.0m, 2, 30_000_000, 7_500_000_000, "Bắc", "Sổ hồng", "Nội thất cao cấp", "Căn bán và thuê đang giữ chỗ.", null, DevelopmentTimeline.AtUtc(2026, 7, 26, 6), 8),
        new("LBV-A-1702", PropertyProject.LumiereBoulevard, "Lumiere Boulevard", PropertyType.Studio, PropertyStatus.Available, 34.0m, 1, 10_000_000, 3_000_000_000, "Đông", "Sổ hồng", "Nội thất cơ bản", "Studio tại Lumiere Boulevard.", new DateOnly(2026, 8, 3), DevelopmentTimeline.AtUtc(2026, 7, 27), 9),
        new("LBV-B-2209", PropertyProject.LumiereBoulevard, "Lumiere Boulevard", PropertyType.TwoBedroomPlus, PropertyStatus.SoonAvailable, 73.0m, 2, 25_000_000, 6_000_000_000, "Tây Nam", "Sổ hồng", "Nội thất đẹp", "Căn sắp trống với nội thất tốt.", new DateOnly(2026, 8, 22), DevelopmentTimeline.AtUtc(2026, 7, 27, 3), 0),
        new("LBV-C-1206", PropertyProject.LumiereBoulevard, "Lumiere Boulevard", PropertyType.OneBedroom, PropertyStatus.Occupied, 48.0m, 1, 18_000_000, null, "Nam", "Sổ hồng", "Nội thất đầy đủ", "Căn đang thuê bởi chuyên gia.", null, DevelopmentTimeline.AtUtc(2026, 7, 27, 5), 1),
        new("LBV-D-3401", PropertyProject.LumiereBoulevard, "Lumiere Boulevard", PropertyType.ThreeBedroomPlus, PropertyStatus.Available, 110.0m, 3, 40_000_000, 12_000_000_000, "Đông Nam", "Sổ hồng", "Nội thất cao cấp", "Căn penthouse tầng cao.", new DateOnly(2026, 8, 5), DevelopmentTimeline.AtUtc(2026, 7, 27, 8), 2, "Hỗ trợ vay theo hồ sơ."),
        new("MCP-A-1508", PropertyProject.MasteriCentrePoint, "Masteri Centre Point", PropertyType.TwoBedroom, PropertyStatus.Occupied, 63.0m, 2, 25_000_000, 6_000_000_000, "Đông", "Sổ hồng", "Nội thất đẹp", "Căn Masteri đang cho thuê.", null, DevelopmentTimeline.AtUtc(2026, 7, 28), 3),
        new("MCP-B-2704", PropertyProject.MasteriCentrePoint, "Masteri Centre Point", PropertyType.ThreeBedroomTwoBathrooms, PropertyStatus.Available, 96.0m, 2, 35_000_000, 9_000_000_000, "Tây", "Sổ hồng", "Nội thất cao cấp", "Căn ba phòng ngủ rộng.", new DateOnly(2026, 8, 4), DevelopmentTimeline.AtUtc(2026, 7, 28, 3), 4),
        new("MCP-C-0809", PropertyProject.MasteriCentrePoint, "Masteri Centre Point", PropertyType.OneBedroomPlus, PropertyStatus.SoonAvailable, 53.0m, 1, 20_000_000, null, "Nam", "Sổ hồng", "Nội thất đầy đủ", "Căn nhỏ sắp bàn giao lại.", new DateOnly(2026, 8, 25), DevelopmentTimeline.AtUtc(2026, 7, 28, 5), 5),
        new("MCP-D-1901", PropertyProject.MasteriCentrePoint, "Masteri Centre Point", PropertyType.TwoBedroomTwoBathrooms, PropertyStatus.Reserved, 72.0m, 2, 30_000_000, 7_500_000_000, "Bắc", "Sổ hồng", "Nội thất đẹp", "Căn đang giữ chỗ cho khách mua.", null, DevelopmentTimeline.AtUtc(2026, 7, 28, 8), 6),
        new("MAN-A-1106", PropertyProject.Manhattan, "Manhattan", PropertyType.TwoBedroom, PropertyStatus.Available, 66.0m, 2, 20_000_000, 5_200_000_000, "Đông Nam", "Sổ hồng", "Nội thất cơ bản", "Căn Manhattan dễ khai thác thuê.", new DateOnly(2026, 7, 31), DevelopmentTimeline.AtUtc(2026, 7, 29), 7),
        new("MAN-B-2402", PropertyProject.Manhattan, "Manhattan", PropertyType.ThreeBedroom, PropertyStatus.Occupied, 89.0m, 2, 30_000_000, null, "Tây Bắc", "Sổ hồng", "Nội thất cao cấp", "Căn đang có khách thuê gia đình.", null, DevelopmentTimeline.AtUtc(2026, 7, 29, 3), 8),
        new("MAN-C-3007", PropertyProject.Manhattan, "Manhattan", PropertyType.OneBedroom, PropertyStatus.Reserved, 47.0m, 1, 15_000_000, 4_200_000_000, "Nam", "Hợp đồng mua bán", "Nội thất cơ bản", "Căn đang giữ chỗ ngắn hạn.", null, DevelopmentTimeline.AtUtc(2026, 7, 29, 5), 9),
        new("MG-A-1609", PropertyProject.ManhattanGlory, "Manhattan Glory", PropertyType.TwoBedroomOneBathroom, PropertyStatus.SoonAvailable, 58.5m, 1, 18_000_000, null, "Đông", "Sổ hồng", "Nội thất đầy đủ", "Căn sắp trống gần tiện ích.", new DateOnly(2026, 8, 16), DevelopmentTimeline.AtUtc(2026, 7, 29, 7), 0),
        new("MG-B-2805", PropertyProject.ManhattanGlory, "Manhattan Glory", PropertyType.ThreeBedroomTwoBathrooms, PropertyStatus.Occupied, 94.0m, 2, 35_000_000, 9_000_000_000, "Tây", "Sổ hồng", "Nội thất cao cấp", "Căn view công viên.", null, DevelopmentTimeline.AtUtc(2026, 7, 29, 9), 1),
        new("MG-C-0703", PropertyProject.ManhattanGlory, "Manhattan Glory", PropertyType.Studio, PropertyStatus.Available, 33.0m, 1, 10_000_000, null, "Bắc", "Hợp đồng mua bán", "Trống", "Studio mới cập nhật ngày hiện tại.", new DateOnly(2026, 7, 30), DevelopmentTimeline.AtUtc(2026, 7, 30), 2),
        new("OP-A-0901", PropertyProject.OpusOne, "Opus One", PropertyType.OneBedroomPlus, PropertyStatus.SoonAvailable, 55.0m, 1, 20_000_000, 5_200_000_000, "Đông Nam", "Sổ hồng", "Nội thất đẹp", "Căn Opus One sáng, phù hợp khách chuyên gia.", new DateOnly(2026, 8, 24), DevelopmentTimeline.AtUtc(2026, 7, 30, 2), 3),
        new("OP-B-2108", PropertyProject.OpusOne, "Opus One", PropertyType.TwoBedroomTwoBathrooms, PropertyStatus.Occupied, 78.0m, 2, 30_000_000, 7_500_000_000, "Nam", "Sổ hồng", "Nội thất cao cấp", "Căn Opus One đang cho thuê tốt.", null, DevelopmentTimeline.AtUtc(2026, 7, 30, 4), 4),
        new("OP-C-3204", PropertyProject.OpusOne, "Opus One", PropertyType.ThreeBedroomPlus, PropertyStatus.SoonAvailable, 112.0m, 3, 40_000_000, 12_000_000_000, "Tây Nam", "Sổ hồng", "Nội thất cao cấp", "Căn lớn sắp trống cuối tháng tới.", new DateOnly(2026, 8, 28), DevelopmentTimeline.AtUtc(2026, 7, 30, 6), 5, "Hỗ trợ vay 60%."),
        new("OP-D-1505", PropertyProject.OpusOne, "Opus One", PropertyType.TwoBedroomPlus, PropertyStatus.Reserved, 76.0m, 2, 25_000_000, 6_000_000_000, "Đông", "Sổ hồng", "Nội thất đẹp", "Căn giữ chỗ để kiểm thử trạng thái Reserved.", null, DevelopmentTimeline.AtUtc(2026, 7, 30, 8), 6)
    ];
}

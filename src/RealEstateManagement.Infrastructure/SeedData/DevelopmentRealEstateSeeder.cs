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

        property.ReplaceImages([
            new PropertyImage(Guid.NewGuid(), propertyId, $"/images/properties/{code.ToLowerInvariant()}.jpg", $"Ảnh căn {code}", 1, true)
        ]);
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
}

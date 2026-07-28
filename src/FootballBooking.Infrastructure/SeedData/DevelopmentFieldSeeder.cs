using FootballBooking.Application.Common.Time;
using FootballBooking.Domain.Fields;
using FootballBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FootballBooking.Infrastructure.SeedData;

public sealed class DevelopmentFieldSeeder(ApplicationDbContext dbContext, ISystemClock clock)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seedFields = BuildSeedFields(clock.UtcNow);

        foreach (var field in seedFields)
        {
            if (await dbContext.Fields.AnyAsync(existing => existing.Code == field.Code, cancellationToken))
            {
                continue;
            }

            await dbContext.Fields.AddAsync(field, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<Field> BuildSeedFields(DateTimeOffset now)
    {
        var fields = new[]
        {
            CreateField(
                "F5A",
                "Sân 5A",
                "san-5a",
                "Sân 5 người",
                10,
                "Sân cỏ nhân tạo mặt êm, phù hợp đội đá phong trào sau giờ làm.",
                "/images/fields/san-5a.svg",
                "Ảnh minh họa Sân 5A",
                250000,
                now),
            CreateField(
                "F5B",
                "Sân 5B",
                "san-5b",
                "Sân 5 người",
                10,
                "Sân gần khu vực chờ, thuận tiện cho đội có trẻ em hoặc người đi cùng.",
                "/images/fields/san-5b.svg",
                "Ảnh minh họa Sân 5B",
                230000,
                now),
            CreateField(
                "F7A",
                "Sân 7A",
                "san-7a",
                "Sân 7 người",
                18,
                "Sân rộng cho đội 7 người, có đèn LED và khu vực khởi động riêng.",
                "/images/fields/san-7a.svg",
                "Ảnh minh họa Sân 7A",
                420000,
                now)
        };

        return fields;
    }

    private static Field CreateField(
        string code,
        string name,
        string slug,
        string fieldType,
        int capacity,
        string description,
        string imagePath,
        string imageAlt,
        long eveningPrice,
        DateTimeOffset now)
    {
        var fieldId = Guid.NewGuid();
        var field = new Field(
            fieldId,
            code,
            name,
            slug,
            fieldType,
            capacity,
            description,
            "12 đường D5, Phường 25, Quận Bình Thạnh",
            """["Cỏ nhân tạo","Đèn LED","Gửi xe miễn phí","Khu vực thay đồ"]""",
            60,
            30,
            FieldStatus.Active,
            now);

        field.ReplaceImages([
            new FieldImage(Guid.NewGuid(), fieldId, imagePath, imageAlt, 1, true, now)
        ]);

        field.ReplaceOperatingHours(Enumerable.Range(0, 7)
            .Select(day => new FieldOperatingHour(Guid.NewGuid(), fieldId, day, false, 360, 1380)));

        field.ReplacePricingRules([
            new PricingRule(Guid.NewGuid(), fieldId, "Giá ban ngày", PricingRuleType.Weekday, null, null, new DateOnly(2026, 1, 1), null, 360, 1080, eveningPrice - 50000, 10, true, now),
            new PricingRule(Guid.NewGuid(), fieldId, "Giá buổi tối", PricingRuleType.Weekday, null, null, new DateOnly(2026, 1, 1), null, 1080, 1380, eveningPrice, 20, true, now),
            new PricingRule(Guid.NewGuid(), fieldId, "Giá cuối tuần", PricingRuleType.Weekend, null, null, new DateOnly(2026, 1, 1), null, 360, 1380, eveningPrice + 50000, 30, true, now)
        ]);

        return field;
    }
}

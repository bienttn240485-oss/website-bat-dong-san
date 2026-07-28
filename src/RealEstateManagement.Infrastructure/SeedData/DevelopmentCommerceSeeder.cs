using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Domain.Bookings;
using RealEstateManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace RealEstateManagement.Infrastructure.SeedData;

public sealed class DevelopmentCommerceSeeder(ApplicationDbContext dbContext, ISystemClock clock)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var services = new[]
        {
            new ServiceItem(Guid.NewGuid(), "BALL", "Thuê bóng", "Bóng thi đấu size 5 đã bơm sẵn.", "quả", 30000, true, 20, true, 10, now),
            new ServiceItem(Guid.NewGuid(), "BIB", "Áo phân đội", "Áo bib hai màu cho đội chưa chuẩn bị đồng phục.", "bộ", 50000, true, 12, true, 20, now),
            new ServiceItem(Guid.NewGuid(), "WATER", "Nước suối", "Thùng 24 chai nước suối dùng trong trận.", "thùng", 90000, true, 30, true, 30, now),
            new ServiceItem(Guid.NewGuid(), "REFEREE", "Thuê trọng tài", "Trọng tài điều hành trận theo khung giờ đặt sân.", "trận", 180000, false, null, true, 40, now)
        };

        foreach (var service in services)
        {
            if (!await dbContext.Services.AnyAsync(existing => existing.Code == service.Code, cancellationToken))
            {
                await dbContext.Services.AddAsync(service, cancellationToken);
            }
        }

        var promotions = new[]
        {
            new PromoCode(Guid.NewGuid(), "ANPHU50", "Giảm 50.000 ₫ cho khách đặt sân mới", PromoDiscountType.FixedAmount, 50000, null, 300000, now.AddDays(-30), now.AddMonths(6), 200, 1, null, null, null, true, now),
            new PromoCode(Guid.NewGuid(), "GIOVANG10", "Giảm 10% cho khung giờ ban ngày", PromoDiscountType.Percentage, 1000, 80000, 250000, now.AddDays(-30), now.AddMonths(3), 100, 2, null, 360, 1080, true, now)
        };

        foreach (var promotion in promotions)
        {
            if (!await dbContext.PromoCodes.AnyAsync(existing => existing.Code == promotion.Code, cancellationToken))
            {
                await dbContext.PromoCodes.AddAsync(promotion, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}


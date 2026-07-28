using FootballBooking.Application.Common.Time;
using FootballBooking.Domain.Bookings;
using FootballBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FootballBooking.Infrastructure.SeedData;

public sealed class DevelopmentOperationsSeeder(ApplicationDbContext dbContext, ISystemClock clock)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        dbContext.PromoCodeUsages.RemoveRange(await dbContext.PromoCodeUsages.ToListAsync(cancellationToken));
        dbContext.Bookings.RemoveRange(await dbContext.Bookings.ToListAsync(cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);

        var field5A = await dbContext.Fields.FirstOrDefaultAsync(field => field.Code == "F5A", cancellationToken);
        var field5B = await dbContext.Fields.FirstOrDefaultAsync(field => field.Code == "F5B", cancellationToken);
        var field7A = await dbContext.Fields.FirstOrDefaultAsync(field => field.Code == "F7A", cancellationToken);
        if (field5A is null || field5B is null || field7A is null)
        {
            return;
        }

        var services = await dbContext.Services.ToDictionaryAsync(service => service.Code, cancellationToken);
        var promotions = await dbContext.PromoCodes.ToDictionaryAsync(promotion => promotion.Code, cancellationToken);
        var now = clock.UtcNow;
        var bookings = BuildBookings(field5A.Id, field5B.Id, field7A.Id, services, promotions, now);

        await dbContext.Bookings.AddRangeAsync(bookings, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<Booking> BuildBookings(
        Guid field5AId,
        Guid field5BId,
        Guid field7AId,
        IReadOnlyDictionary<string, ServiceItem> services,
        IReadOnlyDictionary<string, PromoCode> promotions,
        DateTimeOffset now)
    {
        var rows = new[]
        {
            new SeedBooking("FB-20260720-001", field5AId, new DateOnly(2026, 7, 20), 1080, 1140, "Nguyễn Minh Tuấn", "0901 234 567", "tuan@example.local", BookingStatus.Completed, BookingSource.GuestWeb, 250000, [("BALL", 1)], null, "Khách đặt sân đầu tuần, đã thanh toán tại quầy."),
            new SeedBooking("FB-20260720-002", field5BId, new DateOnly(2026, 7, 20), 1140, 1200, "Trần Quốc Huy", "0902 345 678", "huy@example.local", BookingStatus.Completed, BookingSource.Staff, 230000, [("WATER", 1)], null, "Nhân viên tạo hộ khách quen."),
            new SeedBooking("FB-20260721-001", field7AId, new DateOnly(2026, 7, 21), 1080, 1200, "Lê Hoàng Nam", "0903 456 789", "nam@example.local", BookingStatus.Completed, BookingSource.GuestWeb, 840000, [("REFEREE", 1), ("BIB", 1)], "ANPHU50", "Đội khách dùng sân 7 người và thuê trọng tài."),
            new SeedBooking("FB-20260721-002", field5AId, new DateOnly(2026, 7, 21), 1200, 1260, "Phạm Anh Khoa", "0904 567 890", "khoa@example.local", BookingStatus.Completed, BookingSource.Owner, 250000, [], null, "Chủ sân tạo hộ khách gọi điện."),
            new SeedBooking("FB-20260722-001", field5BId, new DateOnly(2026, 7, 22), 1080, 1140, "Võ Minh Đức", "0905 678 901", "duc@example.local", BookingStatus.Completed, BookingSource.GuestWeb, 230000, [("BALL", 1), ("WATER", 1)], null, "Khách đặt sân và thêm nước uống."),
            new SeedBooking("FB-20260722-002", field7AId, new DateOnly(2026, 7, 22), 1200, 1260, "Đặng Thành Long", "0906 789 012", "long@example.local", BookingStatus.Completed, BookingSource.Staff, 420000, [("REFEREE", 1)], null, "Thanh toán đủ bằng chuyển khoản."),
            new SeedBooking("FB-20260723-001", field5AId, new DateOnly(2026, 7, 23), 1080, 1140, "Bùi Quang Khải", "0907 890 123", "khai@example.local", BookingStatus.Completed, BookingSource.GuestWeb, 250000, [("BIB", 1)], "ANPHU50", "Áp dụng khuyến mãi cho khách mới."),
            new SeedBooking("FB-20260723-002", field5BId, new DateOnly(2026, 7, 23), 1140, 1200, "Hoàng Gia Bảo", "0908 901 234", "bao@example.local", BookingStatus.Completed, BookingSource.WalkIn, 230000, [], null, "Khách đến trực tiếp trong ngày."),
            new SeedBooking("FB-20260724-001", field7AId, new DateOnly(2026, 7, 24), 1080, 1200, "Ngô Hải Đăng", "0909 012 345", "dang@example.local", BookingStatus.Completed, BookingSource.GuestWeb, 840000, [("REFEREE", 1), ("WATER", 1)], null, "Ca sân tối thứ sáu đã hoàn tất."),
            new SeedBooking("FB-20260724-002", field5AId, new DateOnly(2026, 7, 24), 1260, 1320, "Đỗ Nhật Minh", "0910 123 456", "minh@example.local", BookingStatus.Completed, BookingSource.Staff, 250000, [("BALL", 1)], null, "Khách thanh toán tiền mặt."),
            new SeedBooking("FB-20260725-001", field5BId, new DateOnly(2026, 7, 25), 900, 960, "Mai Quốc Việt", "0911 234 567", "viet@example.local", BookingStatus.Completed, BookingSource.GuestWeb, 280000, [("WATER", 1)], "GIOVANG10", "Khung giờ ban ngày cuối tuần."),
            new SeedBooking("FB-20260725-002", field7AId, new DateOnly(2026, 7, 25), 1080, 1200, "Cao Trung Kiên", "0912 345 678", "kien@example.local", BookingStatus.Completed, BookingSource.Owner, 940000, [("REFEREE", 1), ("BIB", 1)], null, "Chủ sân giữ lịch cho giải phong trào."),
            new SeedBooking("FB-20260726-001", field5AId, new DateOnly(2026, 7, 26), 900, 960, "Trịnh Minh Quân", "0913 456 789", "quan@example.local", BookingStatus.PendingPayment, BookingSource.GuestWeb, 300000, [], null, "Khách vừa gửi yêu cầu, chờ ghi nhận cọc."),
            new SeedBooking("FB-20260726-002", field5BId, new DateOnly(2026, 7, 26), 960, 1020, "Hồ Anh Tuấn", "0914 567 890", "anh.tuan@example.local", BookingStatus.Confirmed, BookingSource.GuestWeb, 280000, [("BALL", 1)], null, "Đã nhận cọc, chờ khách đến sân."),
            new SeedBooking("FB-20260726-003", field7AId, new DateOnly(2026, 7, 26), 1020, 1140, "Dương Quốc Thắng", "0915 678 901", "thang@example.local", BookingStatus.CheckedIn, BookingSource.Staff, 940000, [("REFEREE", 1)], null, "Khách đã đến, chuẩn bị bắt đầu ca sân."),
            new SeedBooking("FB-20260726-004", field5AId, new DateOnly(2026, 7, 26), 1080, 1140, "Nguyễn Thanh Tùng", "0916 789 012", "tung@example.local", BookingStatus.InProgress, BookingSource.WalkIn, 300000, [("BALL", 1), ("WATER", 1)], null, "Khách đang sử dụng sân, còn quyết toán cuối ca."),
            new SeedBooking("FB-20260726-005", field5BId, new DateOnly(2026, 7, 26), 1140, 1200, "Lê Đức Huy", "0917 890 123", "duc.huy@example.local", BookingStatus.Completed, BookingSource.Owner, 280000, [("BIB", 1)], null, "Ca sân trong ngày đã hoàn thành."),
            new SeedBooking("FB-20260726-006", field7AId, new DateOnly(2026, 7, 26), 1200, 1320, "Phan Bảo Long", "0918 901 234", "bao.long@example.local", BookingStatus.Cancelled, BookingSource.GuestWeb, 940000, [], null, "Khách hủy trước giờ đá vì thiếu người."),
            new SeedBooking("FB-20260726-007", field5AId, new DateOnly(2026, 7, 26), 1200, 1260, "Vũ Khánh Linh", "0919 012 345", "linh@example.local", BookingStatus.NoShow, BookingSource.Staff, 300000, [], null, "Khách không đến sau thời gian giữ sân."),
            new SeedBooking("FB-20260726-008", field5BId, new DateOnly(2026, 7, 26), 1260, 1320, "Tạ Hoàng Phúc", "0920 123 456", "phuc@example.local", BookingStatus.Expired, BookingSource.GuestWeb, 280000, [], null, "Yêu cầu giữ sân hết hạn do chưa thanh toán.")
        };

        return rows.Select((row, index) => CreateBooking(row, services, promotions, now.AddMinutes(index))).ToArray();
    }

    private static Booking CreateBooking(
        SeedBooking row,
        IReadOnlyDictionary<string, ServiceItem> services,
        IReadOnlyDictionary<string, PromoCode> promotions,
        DateTimeOffset timestamp)
    {
        var serviceLines = row.Services
            .Select(service =>
            {
                if (!services.TryGetValue(service.Code, out var item))
                {
                    return null;
                }

                return new BookingServiceLine(
                    Guid.NewGuid(),
                    Guid.Empty,
                    item.Id,
                    item.Code,
                    item.Name,
                    item.UnitName,
                    item.UnitPrice,
                    service.Quantity,
                    null,
                    timestamp);
            })
            .Where(line => line is not null)
            .Cast<BookingServiceLine>()
            .ToArray();

        var serviceAmount = serviceLines.Sum(line => line.LineTotal);
        promotions.TryGetValue(row.PromoCode ?? string.Empty, out var promoCode);
        var discountAmount = CalculateDiscount(row.CourtAmount + serviceAmount, promoCode);
        var totalAmount = Math.Max(0, row.CourtAmount + serviceAmount - discountAmount);
        DateTimeOffset? expiresAtUtc = row.Status == BookingStatus.PendingPayment ? timestamp.AddMinutes(30) : null;
        var booking = new Booking(
            Guid.NewGuid(),
            row.Code,
            row.FieldId,
            row.Date,
            row.StartMinute,
            row.EndMinute,
            row.CustomerName,
            row.CustomerPhone,
            NormalizePhone(row.CustomerPhone),
            ToDevelopmentEmailDomain(row.CustomerEmail),
            null,
            null,
            row.Source,
            row.Status,
            PaymentStatus.Unpaid,
            row.CourtAmount,
            serviceAmount,
            discountAmount,
            totalAmount,
            promoCode?.Id,
            promoCode?.Code,
            expiresAtUtc,
            timestamp,
            row.Note);

        if (serviceLines.Length > 0 || promoCode is not null)
        {
            var lines = serviceLines
                .Select(line => new BookingServiceLine(
                    Guid.NewGuid(),
                    booking.Id,
                    line.ServiceId,
                    line.ServiceCodeSnapshot,
                    line.ServiceNameSnapshot,
                    line.UnitNameSnapshot,
                    line.UnitPrice,
                    line.Quantity,
                    null,
                    timestamp))
                .ToArray();
            booking.ApplyCommercialSnapshot(lines, promoCode?.Id, promoCode?.Code, serviceAmount, discountAmount, timestamp);
        }

        var paidAmount = row.Status switch
        {
            BookingStatus.Completed => totalAmount,
            BookingStatus.Confirmed or BookingStatus.CheckedIn => Math.Min(totalAmount, Math.Max(100000, totalAmount / 2)),
            BookingStatus.InProgress => Math.Min(totalAmount, Math.Max(150000, totalAmount / 2)),
            BookingStatus.Cancelled => 0,
            BookingStatus.NoShow => 0,
            _ => 0
        };
        if (paidAmount > 0)
        {
            booking.RecordPayment(
                PaymentRecordType.Payment,
                row.Source is BookingSource.GuestWeb ? PaymentMethod.BankTransfer : PaymentMethod.Cash,
                paidAmount,
                PaymentRecordStatus.Succeeded,
                $"GD-{row.Code.Replace("FB-", string.Empty, StringComparison.Ordinal)}",
                row.Status == BookingStatus.Completed ? "Đã thu đủ khi hoàn thành ca sân." : "Đã ghi nhận tiền cọc.",
                null,
                timestamp);
        }

        if (row.Status == BookingStatus.Cancelled)
        {
            booking.Cancel(timestamp, row.Note, 0);
        }

        return booking;
    }

    private static long CalculateDiscount(long subtotal, PromoCode? promoCode)
    {
        if (promoCode is null || subtotal < promoCode.MinimumOrderAmount)
        {
            return 0;
        }

        var discount = promoCode.DiscountType == PromoDiscountType.FixedAmount
            ? promoCode.DiscountValue
            : subtotal * promoCode.DiscountValue / 10000;
        if (promoCode.MaximumDiscountAmount is not null)
        {
            discount = Math.Min(discount, promoCode.MaximumDiscountAmount.Value);
        }

        return Math.Min(subtotal, discount);
    }

    private static string NormalizePhone(string phone)
        => new(phone.Where(char.IsDigit).ToArray());

    private static string ToDevelopmentEmailDomain(string email)
    {
        var atIndex = email.IndexOf('@', StringComparison.Ordinal);
        return atIndex < 0 ? $"{email}@anphu.local" : $"{email[..atIndex]}@anphu.local";
    }

    private sealed record SeedBooking(
        string Code,
        Guid FieldId,
        DateOnly Date,
        int StartMinute,
        int EndMinute,
        string CustomerName,
        string CustomerPhone,
        string CustomerEmail,
        BookingStatus Status,
        BookingSource Source,
        long CourtAmount,
        IReadOnlyList<(string Code, int Quantity)> Services,
        string? PromoCode,
        string Note);
}

using RealEstateManagement.Domain.Bookings;

namespace RealEstateManagement.Application.Bookings;

public static class BookingLabels
{
    public static string Status(BookingStatus status)
        => status switch
        {
            BookingStatus.PendingPayment => "Chá» thanh toán",
            BookingStatus.Confirmed => "ÄÃ£ xác nhận",
            BookingStatus.CheckedIn => "Khách đã đến",
            BookingStatus.InProgress => "Äang sử dụng",
            BookingStatus.Completed => "ÄÃ£ hoÃ n thÃ nh",
            BookingStatus.Cancelled => "ÄÃ£ hủy",
            BookingStatus.NoShow => "Khách không đến",
            BookingStatus.Expired => "ÄÃ£ hết hạn",
            _ => "Không rõ"
        };

    public static string PaymentStatus(PaymentStatus status)
        => status switch
        {
            Domain.Bookings.PaymentStatus.Unpaid => "Chưa thanh toán",
            Domain.Bookings.PaymentStatus.PartiallyPaid => "ÄÃ£ thanh toán một phần",
            Domain.Bookings.PaymentStatus.Paid => "ÄÃ£ thanh toán đủ",
            Domain.Bookings.PaymentStatus.RefundPending => "Äang chá» hoÃ n tiá»n",
            Domain.Bookings.PaymentStatus.PartiallyRefunded => "ÄÃ£ hoÃ n tiá»n một phần",
            Domain.Bookings.PaymentStatus.Refunded => "ÄÃ£ hoÃ n tiá»n",
            Domain.Bookings.PaymentStatus.Failed => "Thanh toán thất bại",
            _ => "Không rõ"
        };

    public static string StatusTone(BookingStatus status)
        => status switch
        {
            BookingStatus.PendingPayment => "warning",
            BookingStatus.Confirmed => "info",
            BookingStatus.CheckedIn => "active",
            BookingStatus.InProgress => "warning",
            BookingStatus.Completed => "success",
            BookingStatus.Cancelled => "danger",
            BookingStatus.NoShow => "neutral",
            BookingStatus.Expired => "neutral",
            _ => "neutral"
        };

    public static string ScheduleTone(BookingStatus status)
        => status switch
        {
            BookingStatus.PendingPayment => "warning",
            BookingStatus.Confirmed => "info",
            BookingStatus.CheckedIn => "active",
            BookingStatus.InProgress => "warning",
            BookingStatus.Completed => "success",
            BookingStatus.Cancelled => "danger",
            BookingStatus.NoShow => "neutral",
            BookingStatus.Expired => "neutral",
            _ => "neutral"
        };

    public static string PaymentRecordType(PaymentRecordType type)
        => type switch
        {
            Domain.Bookings.PaymentRecordType.Payment => "Thu tiá»n",
            Domain.Bookings.PaymentRecordType.Refund => "HoÃ n tiá»n",
            _ => "Không rõ"
        };

    public static string PaymentMethod(PaymentMethod method)
        => method switch
        {
            Domain.Bookings.PaymentMethod.Cash => "Tiá»n mặt",
            Domain.Bookings.PaymentMethod.BankTransfer => "Chuyển khoản",
            Domain.Bookings.PaymentMethod.Online => "Trực tuyến",
            Domain.Bookings.PaymentMethod.Other => "Khác",
            _ => "Không rõ"
        };

    public static string PaymentRecordStatus(PaymentRecordStatus status)
        => status switch
        {
            Domain.Bookings.PaymentRecordStatus.Pending => "Chá» xử lý",
            Domain.Bookings.PaymentRecordStatus.Succeeded => "ÄÃ£ ghi nhận",
            Domain.Bookings.PaymentRecordStatus.Failed => "Thất bại",
            Domain.Bookings.PaymentRecordStatus.Cancelled => "ÄÃ£ hủy",
            _ => "Không rõ"
        };

    public static string PromoDiscountType(PromoDiscountType type)
        => type switch
        {
            Domain.Bookings.PromoDiscountType.Percentage => "Giảm theo phần trăm",
            Domain.Bookings.PromoDiscountType.FixedAmount => "Giảm số tiá»n cố định",
            _ => "Không rõ"
        };
}


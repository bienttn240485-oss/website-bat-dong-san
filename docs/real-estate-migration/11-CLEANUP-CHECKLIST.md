# Checklist dọn dấu vết đặt sân

## Source

- [ ] Không còn Field/Booking/PricingRule/FieldBlock.
- [ ] Không còn ServiceItem/PromoCode/PaymentRecord kiểu booking.
- [ ] Không còn enum trạng thái booking.

## Web

- [ ] Không còn `/fields`, `/booking`, `/booking-lookup`.
- [ ] Không còn menu đặt sân.
- [ ] Không còn chữ “sân bóng”, “khung giờ”, “đặt sân”.

## Admin

- [ ] Không còn quản lý sân, lịch sân, dịch vụ sân, khuyến mãi booking, thanh toán booking.

## Database

- [ ] Đã sao lưu SQLite.
- [ ] Không còn code đọc bảng cũ.
- [ ] Migration xóa bảng cũ đã review.
- [ ] Database mới tạo từ đầu chạy được.

## Tài liệu

- [ ] README đúng bất động sản.
- [ ] Seed đúng Admin/Sale.
- [ ] Dữ liệu demo đúng dự án.

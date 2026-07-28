# Tiêu chí nghiệm thu

## Property

- Mã duy nhất, không nhận mã trùng.
- Lọc theo dự án, phân khu, loại, trạng thái.
- Căn bán chỉ hiện khi `SalePrice > 0`.
- Có ảnh fallback.
- Không xóa khi còn hợp đồng nếu chưa có xử lý rõ ràng.

## LandlordContract

- Không tạo trùng theo ràng buộc một-một.
- Tự tính 12 tháng nếu không nhập hạn.
- Giá nhập không âm.
- Cảnh báo trước 30 ngày.

## TenantContract

- Giá thuê/cọc không âm.
- Thời hạn > 0.
- Tính đúng ngày hết hạn.
- Không cho hai hợp đồng Active chồng nhau.
- Tự cập nhật trạng thái Property.

## Lead

- Khách gửi form không cần đăng nhập.
- Name và Contact bắt buộc.
- Mặc định New.
- Admin phân công được cho Sale.

## Authentication/Authorization

- Chưa đăng nhập không vào `/admin`.
- Admin toàn quyền.
- Sale không quản lý staff.
- Kiểm tra quyền phía server.

## Chất lượng

- Build/test đạt.
- Không mojibake.
- Không còn chuỗi sân bóng ở route mới.
- Không thêm Next.js/Prisma/PostgreSQL.
- Migration chạy được trên SQLite mới.

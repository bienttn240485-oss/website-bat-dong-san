# Chiến lược kiểm thử

## Domain

- Property code/giá/trạng thái.
- LandlordContract hạn, ngày thanh toán, giá nhập.
- TenantContract hạn, thời gian, giá thuê, cọc, trạng thái.
- Lead bắt buộc và chuyển trạng thái.

## Application

- Lọc Property.
- Mã unique.
- Hợp đồng Active không chồng.
- Tự cập nhật PropertyStatus.
- Hàng bán chỉ có SalePrice.
- Dashboard đúng dữ liệu.
- Sale chỉ xử lý lead được phép.

## Infrastructure

- Mapping DateOnly/enum.
- Unique index.
- Quan hệ Property–Contract–Lead–Identity.
- Delete behavior.
- Migration chạy trên SQLite mới.

## Web

- Route public 200.
- Route admin yêu cầu login.
- Authorization Admin/Sale.
- Validation POST.
- Redirect hợp lệ.
- Render chi tiết Property.

## Regression

Không xóa test cũ chỉ để build xanh. Khi bỏ module cũ phải thay bằng test nghiệp vụ mới tương ứng.

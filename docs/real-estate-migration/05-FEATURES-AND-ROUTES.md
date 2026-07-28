# Chức năng và route mục tiêu

## Public

- `GET /` — Trang chủ.
- `GET /properties` — Danh sách căn cho thuê.
- `GET /properties/{id}` — Chi tiết căn cho thuê.
- `GET /sales` — Danh sách căn bán, chỉ lấy `SalePrice > 0`.
- `GET /sales/{id}` — Chi tiết căn bán.
- `GET /contact` — Form liên hệ.
- `POST /contact` — Tạo lead.

## Admin

- `GET /admin/dashboard`
- CRUD `/admin/properties`
- `GET /admin/sales`
- CRUD `/admin/landlord-contracts`
- CRUD `/admin/tenant-contracts`
- Quản lý `/admin/leads`
- Quản lý `/admin/staff`
- Hồ sơ `/admin/settings`

## Route cũ phải loại bỏ sau cùng

- `/fields`
- `/booking`
- `/booking-lookup`
- `/admin/bookings`
- `/admin/fields`
- `/admin/services`
- `/admin/promotions`
- `/admin/payments`
- `/admin/schedule`

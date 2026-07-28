# Kế hoạch triển khai theo pha

## Pha 0 — Bảo vệ trạng thái

Build/test đạt, tạo commit mốc, sao lưu SQLite, ghi nhận route cũ.

## Pha 1 — Domain mới

Tạo Property, PropertyImage, PropertyFurnitureItem, PropertyAmenity, LandlordContract, TenantContract, Lead và enum. Viết unit test. Chưa sửa UI.

## Pha 2 — Application layer

Tạo service, DTO, command, query và interface store cho Property, LandlordContract, TenantContract, Lead và Dashboard. Viết test nghiệp vụ.

## Pha 3 — Infrastructure/database

Thêm DbSet, configuration, repository, migration thêm bảng mới và seed. Không xóa bảng cũ.

## Pha 4 — Admin Property

Danh sách, lọc, thêm, sửa, chi tiết, xóa có kiểm tra quan hệ và quản lý URL ảnh.

## Pha 5 — Hợp đồng

CRUD hợp đồng chủ nhà/khách thuê; tự tính hạn; cảnh báo 30 ngày; giá vào, giá ra, chênh lệch; tự cập nhật trạng thái căn.

## Pha 6 — Lead

Form public, danh sách admin, chi tiết, trạng thái, phân công Sale, liên kết Property.

## Pha 7 — Public website

Trang chủ, căn thuê, căn bán, chi tiết và liên hệ. Chưa cần blog/checkout.

## Pha 8 — Dashboard

Tổng căn, căn trống, căn đã thuê, căn sắp trống, hợp đồng, lead, cảnh báo và chênh lệch.

## Pha 9 — Phân quyền

Admin toàn quyền; Sale xem Property và xử lý Lead được giao; Sale không quản lý staff.

## Pha 10 — Dọn nghiệp vụ cũ

Chỉ khi chức năng mới và test ổn: xóa controller/view/service/entity cũ, tạo migration xóa bảng cũ, cập nhật README.

## Pha 11 — Mở rộng

Upload cloud/local, Excel, email lead, đa ngôn ngữ, blog, biểu đồ GMV.

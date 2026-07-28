# Mẫu prompt giao việc cho Codex

## Tạo Domain

```text
Đọc 00-START-HERE.md, 02-CURRENT-STACK-CONSTRAINTS.md, 03-DOMAIN-MODEL.md và 08-ACCEPTANCE-CRITERIA.md.

Thực hiện Pha 1 trong 07-IMPLEMENTATION-PLAN.md.
Chỉ tạo Domain model và unit test.
Không sửa Controller, Razor View, migration hoặc xóa code cũ.
Không đổi encoding.
Sau khi hoàn thành chạy dotnet build và dotnet test.
Báo cáo file tạo/sửa, quy tắc nghiệp vụ, test và kết quả build/test.
```

## Tạo Application

```text
Thực hiện Pha 2.
Tạo service, DTO, command, query và interface store cho Property, LandlordContract, TenantContract và Lead.
Không triển khai EF Core trong task này.
Không sửa Web.
Không xóa BookingService cũ.
Viết unit test và chạy build/test.
```

## Tạo EF Core migration

```text
Thực hiện Pha 3.
Dùng EF Core và SQLite hiện tại.
Thêm DbSet, configuration và store implementation.
Migration chỉ thêm bảng mới, không xóa bảng sân bóng.
Viết integration test và chạy build/test.
```

## Làm Admin Property

```text
Thực hiện Pha 4.
Tái sử dụng layout, sidebar, Razor component và CSS hiện có.
Không dùng React/Next.js.
Không sửa module hợp đồng và lead ngoài phạm vi.
Form phải validation server-side.
Chạy build/test.
```

## Dọn code cũ

```text
Trước tiên chỉ liệt kê mọi tham chiếu đến Field, Booking, PricingRule, ServiceItem, Promotion, PaymentRecord và FieldBlock.
Phân loại: còn dùng, cần thay, có thể xóa, cần migration.
Chưa xóa gì trong lượt này.
```

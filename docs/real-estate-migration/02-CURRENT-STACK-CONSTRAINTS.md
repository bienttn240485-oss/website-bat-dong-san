# Ràng buộc kiến trúc và tech stack

## Solution hiện tại

```text
src/
  RealEstateManagement.Domain
  RealEstateManagement.Application
  RealEstateManagement.Infrastructure
  RealEstateManagement.Web

tests/
  RealEstateManagement.Tests
```

## Trách nhiệm layer

### Domain
Entity, enum và quy tắc nghiệp vụ cốt lõi. Không phụ thuộc Web hoặc EF Core.

### Application
DTO, command, query, service nghiệp vụ, interface store/repository và validation.

### Infrastructure
EF Core DbContext, configuration, migration, repository, Identity và seed.

### Web
Controller, Area Admin, Razor View, ViewModel, static assets và DI.

### Tests
Unit test, integration test EF Core và web route test.

## Quy tắc bắt buộc

- Không đặt nghiệp vụ phức tạp trong Controller.
- Không truy cập DbContext trực tiếp từ View.
- Không dùng Prisma schema trong code .NET.
- Không tạo API kiểu Next.js.
- Không đổi SQLite sang PostgreSQL trong giai đoạn chuyển đổi.
- Không đổi Identity sang NextAuth.
- Không đổi toàn bộ kiến trúc chỉ để giống dự án tham khảo.
- Không sửa encoding bằng thao tác chuyển mã toàn file.
- Mọi file code và Razor phải lưu UTF-8.

## Quy ước dữ liệu

- Khóa chính dùng `Guid`.
- Tiền VNĐ ưu tiên dùng `long`.
- Ngày không có giờ dùng `DateOnly`.
- Thời điểm hệ thống dùng `DateTimeOffset`.

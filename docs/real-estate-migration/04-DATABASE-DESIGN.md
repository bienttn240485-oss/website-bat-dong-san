# Thiết kế cơ sở dữ liệu EF Core/SQLite

## Bảng chính

- `Properties`
- `PropertyImages`
- `PropertyFurnitureItems`
- `PropertyAmenities`
- `LandlordContracts`
- `TenantContracts`
- `Leads`
- Các bảng Identity hiện có

## Chỉ mục quan trọng

### Properties

- Unique `Code`
- Index `Status`
- Index `Project`
- Index `Area`
- Index `Type`
- Index `AvailableFromDate`

### LandlordContracts

- Unique `PropertyId` nếu giữ quan hệ một-một.
- FK đến `Properties`, delete behavior `Restrict`.

### TenantContracts

- Index `PropertyId`, `Status`, `SignedDate`.
- FK đến `Properties`, delete behavior `Restrict`.

### Leads

- Index `Status`, `CreatedAtUtc`, `PropertyId`, `AssignedToUserId`.
- FK nullable đến `Properties` và `AspNetUsers`.

## Quy tắc migration

1. Tạo bảng mới trước.
2. Không xóa bảng booking cũ trong migration đầu.
3. Chuyển route và UI sang nghiệp vụ mới.
4. Khi test mới đạt và không còn tham chiếu cũ, mới tạo migration xóa bảng cũ.
5. Sao lưu SQLite trước mọi migration phá hủy dữ liệu.

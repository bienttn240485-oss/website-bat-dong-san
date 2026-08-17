# RealEstateManagement

Hệ thống quản lý và giới thiệu bất động sản được xây dựng bằng ASP.NET Core MVC.

Dự án hỗ trợ hai khu vực chính:

* Website công khai cho khách tìm căn thuê, căn bán và gửi yêu cầu tư vấn.
* Khu vực quản trị cho Admin và Sale quản lý căn hộ, hợp đồng, lead, dashboard và phân quyền.

## Chức năng chính

### Website công khai

* Xem danh sách căn hộ cho thuê.
* Xem danh sách căn hộ bán.
* Lọc theo dự án, phân khu, loại căn, trạng thái và khoảng giá.
* Tìm kiếm bằng mã tham chiếu công khai.
* Xem chi tiết căn hộ, hình ảnh, nội thất, tiện ích và thông tin pháp lý.
* Gửi yêu cầu tư vấn từ trang chi tiết căn.
* Gửi biểu mẫu liên hệ không cần đăng nhập.
* Giao diện responsive cho desktop và mobile.

### Khu vực quản trị

* Đăng nhập bằng ASP.NET Core Identity.
* Phân quyền `Admin` và `Sale`.
* Quản lý căn hộ.
* Quản lý hợp đồng chủ nhà.
* Quản lý hợp đồng khách thuê.
* Quản lý và phân công lead.
* Theo dõi trạng thái căn hộ.
* Theo dõi hợp đồng sắp hết hạn.
* Theo dõi giá vào, giá ra và chênh lệch.
* Dashboard tổng hợp tình trạng căn, hợp đồng, lead và số liệu tài chính.
* Quản lý tài khoản nội bộ.

## Tech stack

### Backend

* .NET 10
* ASP.NET Core MVC
* Razor Views
* ASP.NET Core Areas
* ASP.NET Core Identity
* Entity Framework Core
* SQLite

### Frontend

* Tailwind CSS
* Preline
* JavaScript
* Chart.js
* esbuild

### Testing

* xUnit
* ASP.NET Core integration testing
* SQLite test database

## Kiến trúc solution

```text
RealEstateManagement.slnx
├── src/
│   ├── RealEstateManagement.Domain
│   ├── RealEstateManagement.Application
│   ├── RealEstateManagement.Infrastructure
│   └── RealEstateManagement.Web
└── tests/
    └── RealEstateManagement.Tests
```

### Vai trò từng project

* `Domain`: entity, enum và quy tắc nghiệp vụ cốt lõi.
* `Application`: service, DTO, command, query và interface persistence.
* `Infrastructure`: EF Core, SQLite, Identity, migration và seed development.
* `Web`: MVC Controller, Razor View, Admin Area và static assets.
* `Tests`: unit test và integration test.

## Mô hình dữ liệu chính

* `Property`
* `PropertyImage`
* `PropertyFurnitureItem`
* `PropertyAmenity`
* `LandlordContract`
* `TenantContract`
* `Lead`
* `ApplicationUser`

Tiền tệ được lưu bằng `long` theo đơn vị VNĐ. Ngày nghiệp vụ dùng `DateOnly`, còn thời điểm hệ thống dùng `DateTimeOffset`.

## Route chính

### Public

```text
/
/properties
/properties/{id}
/sales
/sales/{id}
/contact
```

### Admin

```text
/admin/login
/admin/dashboard
/admin/properties
/admin/landlord-contracts
/admin/tenant-contracts
/admin/leads
/admin/staff
```

## Yêu cầu môi trường

* .NET 10 SDK
* Node.js và npm
* EF Core CLI nếu cần chạy migration thủ công

Cài EF Core CLI:

```powershell
dotnet tool install --global dotnet-ef
```

## Cài đặt

Clone repository:

Khôi phục package .NET:

```powershell
dotnet restore
```

Cài frontend dependencies:

```powershell
npm install
```

Build frontend assets:

```powershell
npm run build
```

Build solution:

```powershell
dotnet build RealEstateManagement.slnx --no-restore
```

Chạy test:

```powershell
dotnet test RealEstateManagement.slnx --no-build
```

## Cấu hình database

Ứng dụng sử dụng SQLite với connection string mặc định:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=App_Data/real-estate-management.db"
  },
  "Business": {
    "TimeZoneId": "Asia/Ho_Chi_Minh"
  }
}
```

File database local nằm trong:

```text
src/RealEstateManagement.Web/App_Data/
```

Thư mục này không nên được commit lên GitHub.

## Migration

Áp dụng migration:

```powershell
dotnet ef database update `
  --project src/RealEstateManagement.Infrastructure `
  --startup-project src/RealEstateManagement.Web
```

Tạo migration mới:

```powershell
dotnet ef migrations add <MigrationName> `
  --project src/RealEstateManagement.Infrastructure `
  --startup-project src/RealEstateManagement.Web
```

## Cấu hình tài khoản development

Tạo file:

```text
src/RealEstateManagement.Web/appsettings.Development.json
```

Ví dụ tối thiểu:

```json
{
  "SeedOwner": {
    "Email": "admin@anphurealestate.local",
    "Password": "CHANGE_THIS_DEVELOPMENT_PASSWORD",
    "FullName": "Quản trị An Phú"
  }
}
```

Không commit mật khẩu thật hoặc `appsettings.Development.json` lên repository.

Seed dữ liệu development:

```powershell
dotnet run --project src/RealEstateManagement.Web -- --seed-development-data
```

Seeder chỉ được phép chạy trong môi trường `Development`.

Dữ liệu development gồm:

* 1 tài khoản Admin.
* 5 tài khoản Sale.
* Property mẫu.
* Hợp đồng chủ nhà.
* Hợp đồng khách thuê.
* Lead mẫu.
* Dữ liệu phục vụ bộ lọc và dashboard.

## Chạy ứng dụng

```powershell
dotnet run --project src/RealEstateManagement.Web
```

Mở:

```text
http://localhost:5100
```

Cổng thực tế phụ thuộc cấu hình `launchSettings.json`.

## Frontend development

Theo dõi thay đổi CSS và JavaScript:

```powershell
npm run dev
```

Chạy ứng dụng .NET ở terminal khác:

```powershell
dotnet watch --project src/RealEstateManagement.Web
```

## Kiểm thử nhanh

```powershell
dotnet restore
dotnet build RealEstateManagement.slnx --no-restore
dotnet test RealEstateManagement.slnx --no-build
npm run build
```

Các luồng nên kiểm tra thủ công:

1. Tìm căn thuê theo dự án, phân khu và khoảng giá.
2. Mở chi tiết căn và gửi yêu cầu tư vấn.
3. Đăng nhập Admin và phân công lead cho Sale.
4. Sale cập nhật trạng thái lead.
5. Tạo hợp đồng khách thuê.
6. Kiểm tra Property chuyển trạng thái.
7. Kiểm tra dashboard cập nhật dữ liệu.
8. Xác nhận người dùng không có quyền không thể truy cập chức năng Admin.

## Bảo mật và dữ liệu local

Không commit:

* File SQLite.
* `appsettings.Development.json`.
* `.env`.
* Secrets hoặc mật khẩu.
* Log runtime.
* `node_modules`.
* `bin` và `obj`.
* File nội bộ của Codex/agent.
* Script sửa lỗi tạm thời.

## Trạng thái dự án

MVP hiện hỗ trợ:

* Website public cho thuê và bán căn hộ.
* Quản lý Property.
* Quản lý hợp đồng.
* Quản lý Lead.
* Dashboard.
* Phân quyền Admin/Sale.
* SQLite migration và development seed.
* Unit test và integration test.

Các chức năng mở rộng chưa nằm trong MVP:

* Cloud storage và upload ảnh production.
* Gửi email tự động.
* Xuất Excel.
* Blog.
* Đa ngôn ngữ đầy đủ.
* Background jobs và thông báo thời gian thực.

## License



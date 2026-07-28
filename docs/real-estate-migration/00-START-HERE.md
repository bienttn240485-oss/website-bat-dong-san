# Hướng dẫn dùng bộ tài liệu với Codex

## Mục tiêu

Chuyển dự án hiện tại từ hệ thống đặt sân bóng sang hệ thống quản lý và giới thiệu bất động sản, nhưng **giữ nguyên nền tảng kỹ thuật hiện có**.

## Tech stack bắt buộc giữ nguyên

- .NET 10
- ASP.NET Core MVC
- C#
- Entity Framework Core
- SQLite
- ASP.NET Core Identity
- Razor Views
- JavaScript hiện có trong dự án
- CSS/Tailwind hiện có trong dự án
- xUnit

## Không được chuyển sang

- Next.js
- React
- Prisma
- PostgreSQL
- NextAuth
- Node.js backend

Các công nghệ trên chỉ xuất hiện trong tài liệu tham khảo của dự án cũ. Chúng không phải tech stack của dự án đang sửa.

## Cách giao việc cho Codex

Thực hiện theo từng pha trong file `07-IMPLEMENTATION-PLAN.md`.

Mỗi lần chỉ giao một pha hoặc một nhóm task nhỏ. Sau mỗi pha phải:

1. Chạy build.
2. Chạy test.
3. Tóm tắt file đã thay đổi.
4. Nêu migration database đã tạo.
5. Không tự ý xóa nghiệp vụ cũ trước khi nghiệp vụ mới đã thay thế và test đạt.

## Prompt mở đầu đề xuất

```text
Đọc toàn bộ thư mục docs/real-estate-migration trước khi sửa code.

Mục tiêu là chuyển dự án ASP.NET Core MVC hiện tại từ FootballBooking sang hệ thống quản lý bất động sản.

Bắt buộc giữ nguyên tech stack hiện tại:
.NET 10, ASP.NET Core MVC, EF Core, SQLite, Identity, Razor Views, JavaScript/CSS hiện có và xUnit.

Không áp dụng Next.js, Prisma, PostgreSQL hoặc NextAuth dù chúng xuất hiện trong tài liệu đặc tả nguồn.

Làm đúng pha được giao trong 07-IMPLEMENTATION-PLAN.md.
Không sửa ngoài phạm vi.
Không đổi encoding file.
Mọi file văn bản phải lưu UTF-8.
Sau khi hoàn thành phải chạy dotnet build và dotnet test.
```

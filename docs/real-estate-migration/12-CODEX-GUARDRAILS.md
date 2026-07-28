# Rào chắn dành cho Codex

## Được phép

- Tạo class/file mới đúng layer.
- Tái sử dụng component UI.
- Tạo migration EF Core.
- Thêm test.
- Refactor cục bộ đúng task.

## Không được phép

- Chuyển sang Next.js/React/Prisma/PostgreSQL/NextAuth.
- Xóa toàn bộ module cũ trong một lượt.
- Đổi kiến trúc solution.
- Sửa encoding hàng loạt.
- Replace toàn repo `Field -> Property`.
- Xóa test để build xanh.
- Hard-code dashboard.
- Chỉ ẩn nút mà không kiểm tra quyền server.
- Lưu password plain text.
- Thêm package không cần thiết.

## Quy tắc thay đổi

1. Liệt kê file dự định sửa.
2. Chỉ sửa file cần thiết.
3. Không format toàn repo.
4. Giữ UTF-8.
5. Không đổi line ending hàng loạt.
6. Chạy test.
7. Nêu rủi ro còn lại.

## Khi tài liệu và code xung đột

Ưu tiên:

1. Tech stack/kiến trúc hiện tại.
2. Bộ tài liệu migration này.
3. Nghiệp vụ đặc tả.
4. Cách triển khai của project Next.js tham khảo.

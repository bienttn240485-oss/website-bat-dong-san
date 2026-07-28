# Ánh xạ nghiệp vụ cũ sang mới

| Cũ | Mới | Ghi chú |
|---|---|---|
| Field | Property | Không đổi tên máy móc; phải thay mô hình thuộc tính |
| Booking | TenantContract hoặc Lead | Không có ánh xạ một-một |
| Booking customer | Tenant hoặc Lead | Tùy giai đoạn nghiệp vụ |
| Field availability | PropertyStatus | Available, Occupied, SoonAvailable, Reserved |
| PricingRule | MonthlyPrice/SalePrice | Không còn giá theo phút |
| ServiceItem | Furniture/Amenity | Chỉ giữ dữ liệu mô tả |
| Promotion | Bỏ khỏi MVP | Không cần cho lõi |
| PaymentRecord | Bỏ khỏi MVP | Chưa làm thu chi |
| FieldBlock | Bỏ | Dùng trạng thái và hợp đồng |
| Booking lookup | Lead tracking | Thay bằng CRM lead |
| Schedule sân | Contract timeline | Cảnh báo hợp đồng |
| Revenue report | GMV/chênh lệch | Theo hợp đồng |

## Có thể tái sử dụng

Identity, authorization, layout Admin, sidebar/header, Razor component, validation, pagination, EF Core setup, SQLite, test infrastructure và CSS hiện có.

## Phải viết lại

Entity/Service/ViewModel/Controller/View liên quan Field, Booking, PricingRule, ServiceItem, Promotion, Payment và Schedule.

## Cảnh báo

Không replace toàn repo kiểu `Field -> Property` hoặc `Booking -> TenantContract`. Hai miền không tương đương.

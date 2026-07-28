# UI AND ROUTE MAP

## 1. Nguyên tắc UI

- Một ứng dụng, hai layout.
- Website công khai tập trung chuyển đổi booking.
- Admin tập trung tốc độ vận hành và dữ liệu.
- Public mobile-first.
- Admin desktop-first nhưng thao tác cốt lõi phải dùng được trên mobile.
- Tailwind CSS + Preline UI.
- Heroicons.
- Không dùng Bootstrap.
- FullCalendar chỉ dùng cho lịch nội bộ.

---

## 2. Layout

### 2.1. Public layout

```text
Views/Shared/_PublicLayout.cshtml
```

Thành phần:

- Public navbar.
- Flash messages.
- Main content.
- Footer.
- Cookie/privacy notice nếu cần.

Navbar anonymous:

```text
Trang chủ
Sân bóng
Dịch vụ
Khuyến mãi
Tra cứu booking
Liên hệ
Đăng nhập
Đặt sân ngay
```

Navbar Customer:

```text
Trang chủ
Sân bóng
Tra cứu
Booking của tôi
Avatar
```

Navbar Owner/Staff trên public site:

```text
Trang chủ
Sân bóng
Tra cứu
Vào trang quản trị
Avatar
```

Không có nút “Admin Login” công khai.

### 2.2. Admin layout

```text
Areas/Admin/Views/Shared/_AdminLayout.cshtml
```

Thành phần:

- Sidebar.
- Topbar.
- Search/quick action.
- Notification dropdown.
- User menu.
- Breadcrumb.
- Main content.

Owner menu:

```text
Dashboard
Lịch sân
Booking
Thanh toán
Sân
Bảng giá
Giờ hoạt động
Khóa sân & bảo trì
Dịch vụ
Mã giảm giá
Thông báo
Báo cáo
Nhân viên
Nhật ký
Cài đặt
```

Staff menu:

```text
Tổng quan hôm nay
Lịch sân
Booking
Tạo booking
Thanh toán
Dịch vụ phát sinh
Thông báo
```

---

## 3. Public routes

| Method | Route | Controller/Action gợi ý | Quyền | Màn hình |
|---|---|---|---|---|
| GET | `/` | Home/Index | Public | Landing page |
| GET | `/fields` | Fields/Index | Public | Danh sách sân |
| GET | `/fields/{slug}` | Fields/Details | Public | Chi tiết sân |
| GET | `/fields/{id}/availability` | Fields/Availability | Public JSON | Slot trống |
| GET | `/booking` | Booking/Create | Public | Form đặt sân |
| POST | `/booking` | Booking/Create | Public | Tạo booking |
| GET | `/booking/success/{code}` | Booking/Success | Public + token/code | Thành công |
| GET | `/booking/lookup` | BookingLookup/Index | Public | Form tra cứu |
| POST | `/booking/lookup` | BookingLookup/Result | Public | Kết quả tra cứu |
| POST | `/booking/{code}/cancel` | Booking/Cancel | Ownership verified | Hủy booking |
| GET | `/services` | Services/Index | Public | Dịch vụ |
| GET | `/promotions` | Promotions/Index | Public | Khuyến mãi |
| GET | `/policies` | Home/Policies | Public | Chính sách |
| GET | `/contact` | Home/Contact | Public | Liên hệ |
| GET/POST | `/login` | CustomerAuth/Login | Anonymous | Login Customer |
| GET/POST | `/register` | CustomerAuth/Register | Anonymous | Register Customer |
| POST | `/logout` | CustomerAuth/Logout | Authenticated | Đăng xuất |
| GET | `/account` | Account/Index | Customer | Dashboard cá nhân |
| GET | `/account/bookings` | Account/Bookings | Customer | Lịch sử booking |
| GET | `/account/payments` | Account/Payments | Customer | Thanh toán |
| GET | `/account/profile` | Account/Profile | Customer | Hồ sơ |

Route naming cụ thể có thể theo conventional hoặc attribute routing, nhưng URL public phải ổn định.

---

## 4. Admin routes

### 4.1. Authentication

| Method | Route | Quyền | Màn hình |
|---|---|---|---|
| GET/POST | `/admin/login` | Anonymous | Login nội bộ |
| POST | `/admin/logout` | Owner/Staff | Đăng xuất |

### 4.2. Dashboard và schedule

| Method | Route | Quyền | Màn hình |
|---|---|---|---|
| GET | `/admin/dashboard` | Owner/Staff | Dashboard theo role |
| GET | `/admin/schedule` | Owner/Staff | FullCalendar |
| GET | `/admin/api/schedule/events` | Owner/Staff JSON | Event feed |
| GET | `/admin/api/dashboard/*` | Theo role JSON | Chart data |

### 4.3. Booking

| Method | Route | Quyền |
|---|---|---|
| GET | `/admin/bookings` | Owner/Staff |
| GET | `/admin/bookings/{id}` | Owner/Staff |
| GET/POST | `/admin/bookings/create` | Owner/Staff |
| GET/POST | `/admin/bookings/{id}/edit` | Owner/Staff theo status |
| POST | `/admin/bookings/{id}/confirm` | Owner/Staff |
| POST | `/admin/bookings/{id}/check-in` | Owner/Staff |
| POST | `/admin/bookings/{id}/start` | Owner/Staff |
| POST | `/admin/bookings/{id}/complete` | Owner/Staff |
| POST | `/admin/bookings/{id}/no-show` | Owner/Staff |
| POST | `/admin/bookings/{id}/cancel` | Owner/Staff, policy |

### 4.4. Payment

| Method | Route | Quyền |
|---|---|---|
| GET | `/admin/payments` | Owner/Staff |
| POST | `/admin/bookings/{id}/payments` | Owner/Staff |
| POST | `/admin/payments/{id}/refund` | Owner mặc định |

### 4.5. Owner management

| Route prefix | Quyền |
|---|---|
| `/admin/fields` | Owner |
| `/admin/operating-hours` | Owner |
| `/admin/field-blocks` | Owner |
| `/admin/pricing` | Owner |
| `/admin/services` | Owner; Staff chỉ đọc service qua booking flow |
| `/admin/promotions` | Owner |
| `/admin/reports` | Owner |
| `/admin/staff` | Owner |
| `/admin/activity-logs` | Owner |
| `/admin/settings` | Owner |

---

## 5. Màn hình public

### 5.1. Trang chủ

Khối:

- Navbar.
- Hero với CTA “Đặt sân ngay”.
- Tìm nhanh: sân/ngày.
- Sân nổi bật.
- Quy trình đặt sân.
- Dịch vụ.
- Khuyến mãi.
- Tiện ích.
- Review.
- Footer.

### 5.2. Danh sách sân

- Filter loại sân.
- Card ảnh, tên, loại, tiện ích, giá từ.
- Badge trạng thái.
- CTA xem lịch.
- Pagination hoặc danh sách nhỏ.

### 5.3. Chi tiết sân

- Gallery.
- Tên, loại, mô tả.
- Tiện ích.
- Giá tham khảo.
- Date picker.
- Slot selector.
- CTA booking.
- Review.

### 5.4. Booking flow

Bốn bước khuyến nghị:

1. Sân và thời gian.
2. Dịch vụ.
3. Thông tin khách và promo.
4. Xác nhận/thanh toán.

Desktop có summary card bên phải. Mobile có sticky total/CTA.

### 5.5. Booking lookup

Form:

- Booking code.
- Phone.

Kết quả:

- Sân.
- Ngày giờ.
- Status.
- Payment summary.
- Dịch vụ.
- Hủy nếu đủ điều kiện.

Không hiển thị thông tin nhạy cảm vượt nhu cầu.

---

## 6. Màn hình admin

### 6.1. Owner dashboard

Cards:

- Doanh thu hôm nay.
- Doanh thu tháng.
- Booking hôm nay.
- Tiền còn thu.
- Tỷ lệ lấp đầy.
- Tỷ lệ hủy.

Charts:

- Revenue by day.
- Bookings by day.
- Utilization by field.
- Peak hours.

Tables:

- Booking sắp tới.
- Payment cần xử lý.

### 6.2. Staff dashboard

- Timeline hôm nay.
- Sắp bắt đầu.
- Chờ xác nhận.
- Chưa thanh toán đủ.
- Dịch vụ cần chuẩn bị.
- Sân đang khóa.

### 6.3. Schedule

FullCalendar:

- Day/week view.
- Filter Field.
- Event color theo status.
- Background event cho block/maintenance.
- Click slot → create booking.
- Click event → details drawer/modal/page.

Không bật drag/drop thay đổi booking trong MVP nếu chưa có rule rõ; thao tác thay đổi qua form để validation đầy đủ.

### 6.4. Booking list

- Search code/name/phone.
- Filter date, field, status, payment.
- Pagination.
- Quick status badge.
- Action theo quyền.

### 6.5. Forms

- Label rõ.
- Validation inline.
- Không để form quá dài; chia section.
- Destructive action dùng modal xác nhận.

---

## 7. Status visual system

Gợi ý:

| Status | Màu |
|---|---|
| PendingPayment | Xám/vàng nhạt |
| Confirmed | Xanh dương |
| CheckedIn | Tím |
| InProgress | Cam |
| Completed | Xanh lá |
| Cancelled | Đỏ |
| NoShow | Xám đậm |
| Expired | Xám |

Luôn hiển thị text hoặc icon cùng màu.

---

## 8. UI states bắt buộc

Mỗi list/API interaction phải có:

- Loading.
- Empty.
- Error.
- Success feedback.
- Disabled state khi đang submit.

Không để double-submit booking/payment.

---

## 9. Responsive

### Public

- 360px trở lên.
- Booking flow không phụ thuộc hover.
- Date/slot dễ bấm.
- CTA sticky hợp lý.

### Admin

- Sidebar collapse.
- Table có responsive strategy.
- Tác vụ check-in/payment dùng được trên điện thoại.
- Report phức tạp ưu tiên desktop.

---

## 10. Accessibility

- Semantic HTML.
- Label cho input.
- Keyboard navigation.
- Focus visible.
- Contrast phù hợp.
- Icon-only button có accessible name.
- Không dùng màu làm tín hiệu duy nhất.

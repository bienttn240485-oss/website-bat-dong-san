# Mô hình miền

## ApplicationUser

Tiếp tục dùng ASP.NET Core Identity. Có thể bổ sung `DisplayName`, `AvatarUrl`. Vai trò dùng Identity Role: `Admin`, `Sale`.

## Property

Thuộc tính chính:

- `Id: Guid`
- `Code: string`
- `Project: PropertyProject?`
- `Area: string`
- `Type: PropertyType`
- `AreaSize: decimal?`
- `Bathrooms: int?`
- `MonthlyPrice: long?`
- `SalePrice: long?`
- `Direction: string?`
- `LoanInfo: string?`
- `LegalStatus: string?`
- `FurniturePackage: string?`
- `Description: string?`
- `VideoUrl: string?`
- `Status: PropertyStatus`
- `AvailableFromDate: DateOnly?`
- `Notes: string?`
- `CreatedAtUtc`, `UpdatedAtUtc`

Quan hệ: nhiều ảnh, nội thất, tiện ích, hợp đồng khách thuê và lead; tối đa một hợp đồng chủ nhà theo mô hình ban đầu.

## LandlordContract

- `PropertyId`
- `LandlordName`
- `PeCode`
- `SaleName`
- `InputPrice`
- `SignedDate`
- `ExpiryDate`
- `DepositStatus`
- `PaymentDay`
- `PaymentWindow`
- `NextDueDate`
- `Notes`

Quy tắc: giá không âm; hạn sau ngày ký; nếu không nhập hạn thì mặc định 12 tháng; ngày thanh toán từ 1 đến 31.

## TenantContract

- `PropertyId`
- `TenantName`
- `ManagerName`
- `RentalPrice`
- `SignedDate`
- `TermMonths`
- `DepositAmount`
- `DepositReturnDate`
- `PeCode`
- `PassCode`
- `Status`
- `Notes`

`ExpiryDate = SignedDate + TermMonths`.

Không cho hai hợp đồng `Active` chồng thời gian trên cùng Property. Khi hợp đồng có hiệu lực, Property thường chuyển `Occupied`; gần hết hạn chuyển `SoonAvailable`; hết hợp đồng và không còn hợp đồng Active thì chuyển `Available`.

## Lead

- `Name`
- `Contact`
- `PropertyId?`
- `Subject?`
- `Message?`
- `Language`
- `Status`
- `AssignedToUserId?`
- `CreatedAtUtc`, `UpdatedAtUtc`

Lead mới mặc định `New`.

## Enum

### PropertyType
`Studio`, `OneBedroom`, `OneBedroomPlus`, `TwoBedroom`, `TwoBedroomPlus`, `TwoBedroomOneBathroom`, `TwoBedroomTwoBathrooms`, `ThreeBedroom`, `ThreeBedroomTwoBathrooms`, `ThreeBedroomPlus`.

### PropertyStatus
`Available`, `Occupied`, `SoonAvailable`, `Reserved`.

### DepositStatus
`NoExtension`, `Supplemented`, `Pending`, `NotApplicable`.

### ContractStatus
`Active`, `Expired`, `Cancelled`, `Renewed`.

### LeadStatus
`New`, `Contacted`, `Viewing`, `Converted`, `Lost`.

### PropertyProject
`VinhomesGrandPark`, `Origami`, `GloryHeights`, `Beverly`, `BeverlySolari`, `LumiereBoulevard`, `TheRainbow`, `Manhattan`, `ManhattanGlory`, `MasteriCentrePoint`, `OpusOne`.

using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Contracts;

public sealed class TenantContractService(ITenantContractStore store, ISystemClock clock) : ITenantContractService
{
    private const int SoonAvailableDays = 30;

    public Task<IReadOnlyList<TenantContractDto>> ListTenantContractsAsync(ContractFilterQuery query, CancellationToken cancellationToken = default)
        => store.ListTenantContractsAsync(query, cancellationToken);

    public Task<TenantContractDto?> GetTenantContractAsync(Guid id, CancellationToken cancellationToken = default)
        => store.GetTenantContractAsync(id, cancellationToken);

    public Task<IReadOnlyList<TenantContractDto>> ListTenantContractsForPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
        => store.ListTenantContractsForPropertyAsync(propertyId, cancellationToken);

    public async Task<ContractCommandResult> CreateTenantContractAsync(TenantContractEditorCommand command, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateEditorAsync(command, null, cancellationToken);
        if (errors.Count > 0)
        {
            return ContractCommandResult.Failure(errors);
        }

        var now = clock.UtcNow;
        var contract = new TenantContract(
            Guid.NewGuid(),
            command.PropertyId,
            command.TenantName,
            command.ManagerName,
            command.RentalPrice,
            command.SignedDate,
            command.TermMonths,
            command.DepositAmount,
            command.DepositReturnDate,
            command.PeCode,
            command.PassCode,
            command.Status,
            command.Notes,
            now);

        await store.AddTenantContractAsync(contract, cancellationToken);
        await SyncPropertyStatusAsync(command.PropertyId, contract, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return ContractCommandResult.Success(contract.Id);
    }

    public async Task<ContractCommandResult> UpdateTenantContractAsync(Guid contractId, TenantContractEditorCommand command, CancellationToken cancellationToken = default)
    {
        var contract = await store.GetTenantContractForUpdateAsync(contractId, cancellationToken);
        if (contract is null)
        {
            return ContractCommandResult.Failure(["Không tìm thấy hợp đồng khách thuê cần cập nhật."]);
        }

        if (command.PropertyId != contract.PropertyId)
        {
            return ContractCommandResult.Failure(["Không thể chuyển hợp đồng khách thuê sang căn hộ khác."]);
        }

        var errors = await ValidateEditorAsync(command, contractId, cancellationToken);
        if (errors.Count > 0)
        {
            return ContractCommandResult.Failure(errors);
        }

        var now = clock.UtcNow;
        contract.UpdateDetails(
            command.TenantName,
            command.ManagerName,
            command.RentalPrice,
            command.SignedDate,
            command.TermMonths,
            command.DepositAmount,
            command.DepositReturnDate,
            command.PeCode,
            command.PassCode,
            command.Notes,
            now);
        contract.ChangeStatus(command.Status, now);

        await SyncPropertyStatusAsync(command.PropertyId, contract, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return ContractCommandResult.Success(contract.Id);
    }

    public async Task<ContractCommandResult> ChangeStatusAsync(TenantContractStatusCommand command, CancellationToken cancellationToken = default)
    {
        var contract = await store.GetTenantContractForUpdateAsync(command.TenantContractId, cancellationToken);
        if (contract is null)
        {
            return ContractCommandResult.Failure(["Không tìm thấy hợp đồng khách thuê cần cập nhật trạng thái."]);
        }

        if (command.Status == ContractStatus.Active && await HasOverlappingActiveContractAsync(contract.PropertyId, contract.SignedDate, contract.ExpiryDate, contract.Id, cancellationToken))
        {
            return ContractCommandResult.Failure(["Căn hộ đã có hợp đồng khách thuê đang hiệu lực trong khoảng thời gian này."]);
        }

        contract.ChangeStatus(command.Status, clock.UtcNow);
        await SyncPropertyStatusAsync(contract.PropertyId, contract, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return ContractCommandResult.Success(contract.Id);
    }

    public async Task<ContractCommandResult> DeleteTenantContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var contract = await store.GetTenantContractAsync(contractId, cancellationToken);
        if (contract is null)
        {
            return ContractCommandResult.Failure(["Không tìm thấy hợp đồng khách thuê cần xóa."]);
        }

        return ContractCommandResult.Failure(["Không xóa hợp đồng khách thuê để giữ lịch sử quản lý. Hãy đổi trạng thái sang Đã hủy nếu hợp đồng không còn hiệu lực."]);
    }

    private async Task<List<string>> ValidateEditorAsync(TenantContractEditorCommand command, Guid? contractId, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (command.PropertyId == Guid.Empty)
        {
            errors.Add("Vui lòng chọn căn hộ.");
        }
        else if (await store.GetPropertyForUpdateAsync(command.PropertyId, cancellationToken) is null)
        {
            errors.Add("Không tìm thấy căn hộ cho hợp đồng khách thuê.");
        }

        Required(command.TenantName, "Vui lòng nhập tên khách thuê.", errors);
        if (command.RentalPrice < 0)
        {
            errors.Add("Giá thuê không được âm.");
        }

        if (command.DepositAmount < 0)
        {
            errors.Add("Tiền cọc không được âm.");
        }

        if (command.TermMonths <= 0)
        {
            errors.Add("Thời hạn thuê phải lớn hơn 0 tháng.");
        }

        if (command.Status == ContractStatus.Active
            && command.TermMonths > 0
            && await HasOverlappingActiveContractAsync(command.PropertyId, command.SignedDate, command.SignedDate.AddMonths(command.TermMonths), contractId, cancellationToken))
        {
            errors.Add("Căn hộ đã có hợp đồng khách thuê đang hiệu lực trong khoảng thời gian này.");
        }

        return errors;
    }

    private async Task<bool> HasOverlappingActiveContractAsync(Guid propertyId, DateOnly signedDate, DateOnly expiryDate, Guid? exceptContractId, CancellationToken cancellationToken)
    {
        var activeContracts = await store.ListActiveTenantContractsAsync(propertyId, exceptContractId, cancellationToken);
        return activeContracts.Any(contract => contract.Overlaps(signedDate, expiryDate));
    }

    private async Task SyncPropertyStatusAsync(Guid propertyId, TenantContract? currentContract, CancellationToken cancellationToken)
    {
        var property = await store.GetPropertyForUpdateAsync(propertyId, cancellationToken);
        if (property is null || property.Status == PropertyStatus.Reserved)
        {
            return;
        }

        var today = TodayInBusinessTimeZone();
        var activeContracts = (await store.ListActiveTenantContractsAsync(propertyId, currentContract?.Id, cancellationToken))
            .Where(contract => contract.ExpiryDate > today)
            .ToList();

        if (currentContract?.Status == ContractStatus.Active && currentContract.ExpiryDate > today)
        {
            activeContracts.Add(currentContract);
        }

        if (activeContracts.Count == 0)
        {
            property.ChangeStatus(PropertyStatus.Available, clock.UtcNow);
            return;
        }

        property.ChangeStatus(activeContracts.Any(contract => contract.ExpiryDate <= today.AddDays(SoonAvailableDays))
            ? PropertyStatus.SoonAvailable
            : PropertyStatus.Occupied, clock.UtcNow);
    }

    private DateOnly TodayInBusinessTimeZone()
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, BusinessTimeZone()).DateTime);

    private static TimeZoneInfo BusinessTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static void Required(string? value, string message, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(message);
        }
    }
}
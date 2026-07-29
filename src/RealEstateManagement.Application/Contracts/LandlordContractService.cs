using RealEstateManagement.Application.Common.Time;
using RealEstateManagement.Domain.Contracts;

namespace RealEstateManagement.Application.Contracts;

public sealed class LandlordContractService(ILandlordContractStore store, ISystemClock clock) : ILandlordContractService
{
    public Task<IReadOnlyList<LandlordContractDto>> ListLandlordContractsAsync(ContractFilterQuery query, CancellationToken cancellationToken = default)
        => store.ListLandlordContractsAsync(query, cancellationToken);

    public Task<LandlordContractDto?> GetLandlordContractAsync(Guid id, CancellationToken cancellationToken = default)
        => store.GetLandlordContractAsync(id, cancellationToken);

    public async Task<ContractCommandResult> CreateLandlordContractAsync(LandlordContractEditorCommand command, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateEditorAsync(command, null, cancellationToken);
        if (errors.Count > 0)
        {
            return ContractCommandResult.Failure(errors);
        }

        var contract = new LandlordContract(
            Guid.NewGuid(),
            command.PropertyId,
            command.LandlordName,
            command.PeCode,
            command.SaleName,
            command.InputPrice,
            command.SignedDate,
            command.ExpiryDate,
            command.DepositStatus,
            command.PaymentDay,
            command.PaymentWindow,
            command.NextDueDate,
            command.Notes,
            clock.UtcNow);

        await store.AddLandlordContractAsync(contract, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return ContractCommandResult.Success(contract.Id);
    }

    public async Task<ContractCommandResult> UpdateLandlordContractAsync(Guid contractId, LandlordContractEditorCommand command, CancellationToken cancellationToken = default)
    {
        var contract = await store.GetLandlordContractForUpdateAsync(contractId, cancellationToken);
        if (contract is null)
        {
            return ContractCommandResult.Failure(["Không tìm thấy hợp đồng chủ nhà cần cập nhật."]);
        }

        var errors = await ValidateEditorAsync(command, contractId, cancellationToken);
        if (errors.Count > 0)
        {
            return ContractCommandResult.Failure(errors);
        }

        contract.UpdateDetails(
            command.LandlordName,
            command.PeCode,
            command.SaleName,
            command.InputPrice,
            command.SignedDate,
            command.ExpiryDate,
            command.DepositStatus,
            command.PaymentDay,
            command.PaymentWindow,
            command.NextDueDate,
            command.Notes,
            clock.UtcNow);

        await store.SaveChangesAsync(cancellationToken);

        return ContractCommandResult.Success(contract.Id);
    }

    private async Task<List<string>> ValidateEditorAsync(LandlordContractEditorCommand command, Guid? contractId, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (command.PropertyId == Guid.Empty)
        {
            errors.Add("Vui lòng chọn căn hộ.");
        }
        else if (!await store.PropertyExistsAsync(command.PropertyId, cancellationToken))
        {
            errors.Add("Không tìm thấy căn hộ cho hợp đồng chủ nhà.");
        }
        else if (await store.ContractExistsForPropertyAsync(command.PropertyId, contractId, cancellationToken))
        {
            errors.Add("Căn hộ này đã có hợp đồng chủ nhà.");
        }

        Required(command.LandlordName, "Vui lòng nhập tên chủ nhà.", errors);
        if (command.InputPrice < 0)
        {
            errors.Add("Giá vào không được âm.");
        }

        var expiryDate = command.ExpiryDate ?? command.SignedDate.AddMonths(12);
        if (expiryDate <= command.SignedDate)
        {
            errors.Add("Ngày hết hạn phải sau ngày ký.");
        }

        if (command.PaymentDay is < 1 or > 31)
        {
            errors.Add("Ngày thanh toán phải từ 1 đến 31.");
        }

        return errors;
    }

    private static void Required(string? value, string message, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(message);
        }
    }
}

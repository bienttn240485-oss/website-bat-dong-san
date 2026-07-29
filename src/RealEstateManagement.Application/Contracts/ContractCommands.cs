using RealEstateManagement.Domain.Contracts;

namespace RealEstateManagement.Application.Contracts;

public sealed record LandlordContractEditorCommand(
    Guid PropertyId,
    string LandlordName,
    string? PeCode,
    string? SaleName,
    long InputPrice,
    DateOnly SignedDate,
    DateOnly? ExpiryDate,
    DepositStatus DepositStatus,
    int? PaymentDay,
    string? PaymentWindow,
    DateOnly? NextDueDate,
    string? Notes);

public sealed record TenantContractEditorCommand(
    Guid PropertyId,
    string TenantName,
    string? ManagerName,
    long RentalPrice,
    DateOnly SignedDate,
    int TermMonths,
    long DepositAmount,
    DateOnly? DepositReturnDate,
    string? PeCode,
    string? PassCode,
    ContractStatus Status,
    string? Notes);

public sealed record TenantContractStatusCommand(Guid TenantContractId, ContractStatus Status);

public sealed record ContractCommandResult(bool Succeeded, Guid? ContractId, IReadOnlyList<string> Errors)
{
    public static ContractCommandResult Success(Guid contractId) => new(true, contractId, []);

    public static ContractCommandResult Failure(IEnumerable<string> errors) => new(false, null, errors.ToArray());
}

using RealEstateManagement.Domain.Contracts;

namespace RealEstateManagement.Application.Contracts;

public sealed record ContractFilterQuery(
    Guid? PropertyId = null,
    ContractStatus? Status = null,
    DateOnly? ExpiringBefore = null);

public sealed record LandlordContractDto(
    Guid Id,
    Guid PropertyId,
    string LandlordName,
    string? PeCode,
    string? SaleName,
    long InputPrice,
    DateOnly SignedDate,
    DateOnly ExpiryDate,
    DepositStatus DepositStatus,
    int? PaymentDay,
    string? PaymentWindow,
    DateOnly? NextDueDate,
    string? Notes);

public sealed record TenantContractDto(
    Guid Id,
    Guid PropertyId,
    string TenantName,
    string? ManagerName,
    long RentalPrice,
    DateOnly SignedDate,
    int TermMonths,
    DateOnly ExpiryDate,
    long DepositAmount,
    DateOnly? DepositReturnDate,
    string? PeCode,
    string? PassCode,
    ContractStatus Status,
    string? Notes);

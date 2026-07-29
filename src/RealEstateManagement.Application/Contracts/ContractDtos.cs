using RealEstateManagement.Domain.Contracts;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Contracts;

public sealed record ContractFilterQuery(
    Guid? PropertyId = null,
    string? Keyword = null,
    PropertyProject? Project = null,
    string? Area = null,
    ContractStatus? Status = null,
    DepositStatus? DepositStatus = null,
    DateOnly? ExpiringBefore = null,
    bool ExpiredOnly = false);

public sealed record LandlordContractDto(
    Guid Id,
    Guid PropertyId,
    string PropertyCode,
    PropertyProject? Project,
    string Area,
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
    string PropertyCode,
    PropertyProject? Project,
    string Area,
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

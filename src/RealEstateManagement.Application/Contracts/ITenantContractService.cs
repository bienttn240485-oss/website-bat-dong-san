namespace RealEstateManagement.Application.Contracts;

public interface ITenantContractService
{
    Task<IReadOnlyList<TenantContractDto>> ListTenantContractsAsync(ContractFilterQuery query, CancellationToken cancellationToken = default);
    Task<TenantContractDto?> GetTenantContractAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantContractDto>> ListTenantContractsForPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task<ContractCommandResult> CreateTenantContractAsync(TenantContractEditorCommand command, CancellationToken cancellationToken = default);
    Task<ContractCommandResult> UpdateTenantContractAsync(Guid contractId, TenantContractEditorCommand command, CancellationToken cancellationToken = default);
    Task<ContractCommandResult> ChangeStatusAsync(TenantContractStatusCommand command, CancellationToken cancellationToken = default);
    Task<ContractCommandResult> DeleteTenantContractAsync(Guid contractId, CancellationToken cancellationToken = default);
}

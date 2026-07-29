namespace RealEstateManagement.Application.Contracts;

public interface ILandlordContractService
{
    Task<IReadOnlyList<LandlordContractDto>> ListLandlordContractsAsync(ContractFilterQuery query, CancellationToken cancellationToken = default);
    Task<LandlordContractDto?> GetLandlordContractAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LandlordContractDto?> GetLandlordContractForPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task<ContractCommandResult> CreateLandlordContractAsync(LandlordContractEditorCommand command, CancellationToken cancellationToken = default);
    Task<ContractCommandResult> UpdateLandlordContractAsync(Guid contractId, LandlordContractEditorCommand command, CancellationToken cancellationToken = default);
    Task<ContractCommandResult> DeleteLandlordContractAsync(Guid contractId, CancellationToken cancellationToken = default);
}

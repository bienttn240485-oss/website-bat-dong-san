namespace RealEstateManagement.Application.Fields;

public interface IFieldService
{
    Task<IReadOnlyList<FieldSummaryDto>> ListPublicFieldsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FieldSummaryDto>> ListAdminFieldsAsync(CancellationToken cancellationToken = default);
    Task<FieldDetailDto?> GetFieldDetailBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<FieldDetailDto?> GetFieldDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FieldCommandResult> CreateFieldAsync(FieldEditorCommand command, CancellationToken cancellationToken = default);
    Task<FieldCommandResult> UpdateFieldAsync(Guid fieldId, FieldEditorCommand command, CancellationToken cancellationToken = default);
    Task<FieldCommandResult> AddBlockAsync(FieldBlockCommand command, CancellationToken cancellationToken = default);
}


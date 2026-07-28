using RealEstateManagement.Domain.Fields;

namespace RealEstateManagement.Application.Fields;

public interface IFieldStore
{
    Task<IReadOnlyList<FieldSummaryDto>> ListPublicFieldsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<FieldSummaryDto>> ListAdminFieldsAsync(CancellationToken cancellationToken);
    Task<FieldDetailDto?> GetFieldDetailBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<FieldDetailDto?> GetFieldDetailByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Field?> GetFieldForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, Guid? exceptFieldId, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, Guid? exceptFieldId, CancellationToken cancellationToken);
    Task AddFieldAsync(Field field, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}


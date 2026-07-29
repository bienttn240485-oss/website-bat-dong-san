using RealEstateManagement.Domain.Leads;

namespace RealEstateManagement.Application.Leads;

public sealed record LeadFilterQuery(
    LeadStatus? Status = null,
    Guid? PropertyId = null,
    Guid? AssignedToUserId = null,
    DateOnly? CreatedFrom = null,
    DateOnly? CreatedTo = null,
    string? Keyword = null);

public sealed record LeadDto(
    Guid Id,
    string Name,
    string Contact,
    Guid? PropertyId,
    string? Subject,
    string? Message,
    string Language,
    LeadStatus Status,
    Guid? AssignedToUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

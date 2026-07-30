using RealEstateManagement.Domain.Leads;
using RealEstateManagement.Domain.Properties;

namespace RealEstateManagement.Application.Leads;

public sealed record LeadFilterQuery(
    LeadStatus? Status = null,
    Guid? PropertyId = null,
    Guid? AssignedToUserId = null,
    DateOnly? CreatedFrom = null,
    DateOnly? CreatedTo = null,
    string? Keyword = null,
    PropertyProject? Project = null,
    string? Area = null,
    string? Language = null,
    bool UnassignedOnly = false,
    bool NewestFirst = true);

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
    DateTimeOffset UpdatedAtUtc,
    string? PropertyCode = null,
    PropertyProject? PropertyProject = null,
    string? PropertyArea = null,
    PropertyType? PropertyType = null,
    long? PropertyMonthlyPrice = null,
    long? PropertySalePrice = null,
    PropertyStatus? PropertyStatus = null,
    string? AssignedToDisplayName = null,
    string? AssignedToEmail = null);

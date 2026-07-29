using RealEstateManagement.Domain.Leads;

namespace RealEstateManagement.Application.Leads;

public sealed record LeadCreateCommand(
    string Name,
    string Contact,
    Guid? PropertyId,
    string? Subject,
    string? Message,
    string? Language);

public sealed record LeadUpdateCommand(
    string Name,
    string Contact,
    Guid? PropertyId,
    string? Subject,
    string? Message,
    string? Language);

public sealed record LeadStatusCommand(Guid LeadId, LeadStatus Status);

public sealed record LeadAssignmentCommand(Guid LeadId, Guid SaleUserId);

public sealed record LeadCommandResult(bool Succeeded, Guid? LeadId, IReadOnlyList<string> Errors)
{
    public static LeadCommandResult Success(Guid leadId) => new(true, leadId, []);

    public static LeadCommandResult Failure(IEnumerable<string> errors) => new(false, null, errors.ToArray());
}

namespace RealEstateManagement.Application.Leads;

public interface ILeadService
{
    Task<IReadOnlyList<LeadDto>> ListLeadsAsync(LeadFilterQuery query, CancellationToken cancellationToken = default);
    Task<LeadDto?> GetLeadAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeadCommandResult> CreateLeadAsync(LeadCreateCommand command, CancellationToken cancellationToken = default);
    Task<LeadCommandResult> UpdateLeadAsync(Guid leadId, LeadUpdateCommand command, CancellationToken cancellationToken = default);
    Task<LeadCommandResult> ChangeStatusAsync(LeadStatusCommand command, CancellationToken cancellationToken = default);
    Task<LeadCommandResult> AssignLeadAsync(LeadAssignmentCommand command, CancellationToken cancellationToken = default);
}

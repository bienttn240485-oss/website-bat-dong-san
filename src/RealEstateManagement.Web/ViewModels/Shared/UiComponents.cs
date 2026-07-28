namespace RealEstateManagement.Web.ViewModels.Shared;

public sealed record HeroIconViewModel(string Name, string CssClass = "h-5 w-5");

public sealed record BreadcrumbItemViewModel(string Label, string? Url = null);

public sealed record ButtonViewModel(
    string Label,
    string? Url = null,
    string Style = "primary",
    string Type = "button",
    string? IconName = null,
    bool Disabled = false);

public sealed record FormFieldViewModel(
    string Id,
    string Label,
    string Type = "text",
    string? Value = null,
    string? Placeholder = null,
    string? Hint = null,
    bool Required = false);

public sealed record AlertViewModel(
    string Title,
    string Message,
    string Tone = "info",
    string? IconName = null);

public sealed record EmptyStateViewModel(
    string Title,
    string Message,
    string? ActionLabel = null,
    string? ActionUrl = null,
    string IconName = "clipboard-document-list");

public sealed record StatusBadgeViewModel(string Label, string Tone = "neutral");

public sealed record PaginationShellViewModel(int CurrentPage, int TotalPages);

public sealed record ConfirmationModalViewModel(
    string Id,
    string Title,
    string Message,
    string ConfirmLabel,
    string CancelLabel = "Hủy");


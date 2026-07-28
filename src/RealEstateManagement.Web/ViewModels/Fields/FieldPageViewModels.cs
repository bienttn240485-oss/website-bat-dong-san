using RealEstateManagement.Application.Fields;

namespace RealEstateManagement.Web.ViewModels.Fields;

public sealed record FieldListPageViewModel(IReadOnlyList<FieldSummaryDto> Fields);

public sealed record FieldDetailPageViewModel(FieldDetailDto Field);


using FootballBooking.Application.Fields;

namespace FootballBooking.Web.ViewModels.Fields;

public sealed record FieldListPageViewModel(IReadOnlyList<FieldSummaryDto> Fields);

public sealed record FieldDetailPageViewModel(FieldDetailDto Field);

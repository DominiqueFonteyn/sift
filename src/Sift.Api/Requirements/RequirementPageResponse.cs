namespace Sift.Api.Requirements;

public sealed record RequirementPageResponse(IReadOnlyList<RequirementResponse> Items, int Total, int Page, int PageSize);

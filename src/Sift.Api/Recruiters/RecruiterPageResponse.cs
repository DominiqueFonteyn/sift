namespace Sift.Api.Recruiters;

public sealed record RecruiterPageResponse(IReadOnlyList<RecruiterResponse> Items, int Total, int Page, int PageSize);

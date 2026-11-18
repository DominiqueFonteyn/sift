namespace Sift.Api.Bureaus;

public sealed record BureauPageResponse(IReadOnlyList<BureauResponse> Items, int Total, int Page, int PageSize);

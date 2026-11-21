using System.Text.Json.Serialization;

namespace Sift.Api.Recruiters;

// The bureau property is omitted entirely when there is no link, as the contract says.
public sealed record RecruiterResponse(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? LinkedInUrl,
    DateTimeOffset CreatedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RecruiterBureauResponse? Bureau = null);

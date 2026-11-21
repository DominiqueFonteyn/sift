using System.Text.Json.Serialization;

namespace Sift.Api.Bureaus;

// The contract promises no website property at all when none was given, not a null.
public sealed record BureauResponse(
    Guid Id,
    string Name,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Website,
    DateTimeOffset CreatedAt);

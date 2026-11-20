namespace Sift.Domain.Requirements;

// The wire names are part of the API contract (requirements.tsp).
public static class RequirementKinds
{
    public const string MustHave = "must-have";

    public const string NiceToHave = "nice-to-have";

    public static bool TryParse(string? value, out RequirementKind kind)
    {
        switch (value)
        {
            case MustHave:
                kind = RequirementKind.MustHave;
                return true;
            case NiceToHave:
                kind = RequirementKind.NiceToHave;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    public static RequirementKind Parse(string value) =>
        TryParse(value, out var kind)
            ? kind
            : throw new FormatException($"Unknown requirement kind '{value}'.");

    public static string ToName(RequirementKind kind) => kind switch
    {
        RequirementKind.MustHave => MustHave,
        RequirementKind.NiceToHave => NiceToHave,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown requirement kind.")
    };
}

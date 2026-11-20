using System.Text.RegularExpressions;
using Sift.Domain.Requirements;

namespace Sift.Domain.Matching;

// The MVP rule engine (MVP.md, decision 2): a requirement is met when its term is found in the vacancy text.
// The resume is a prerequisite for scoring elsewhere, but its text plays no part here.
public static partial class VacancyScorer
{
    public const int PassThreshold = 50;

    public const int UnmetMustHaveCap = 49;

    public const int MaxScore = 100;

    public static MatchResult Score(string vacancyText, IReadOnlyList<MatchRequirement> requirements)
    {
        ArgumentNullException.ThrowIfNull(vacancyText);
        ArgumentNullException.ThrowIfNull(requirements);

        if (requirements.Count == 0)
        {
            throw new ArgumentException("At least one requirement is needed to score a vacancy.", nameof(requirements));
        }

        var text = Normalise(vacancyText);
        var verdicts = requirements.Select(requirement => Judge(text, requirement)).ToList();

        var mustHaves = verdicts.Where(v => v.Kind == RequirementKind.MustHave).ToList();
        var niceToHaves = verdicts.Where(v => v.Kind == RequirementKind.NiceToHave).ToList();
        var unmetMustHaves = mustHaves.Count(v => v.Verdict == Verdict.NotMet);
        var metNiceToHaves = niceToHaves.Count(v => v.Verdict == Verdict.Met);

        int score;
        if (unmetMustHaves == 0)
        {
            // Every must-have is met: the pass line, plus a share of the remaining points for the nice-to-haves.
            score = niceToHaves.Count == 0
                ? MaxScore
                : PassThreshold + ((MaxScore - PassThreshold) * metNiceToHaves / niceToHaves.Count);
        }
        else
        {
            // Below the pass line every requirement counts equally; the share can never reach the cap's ceiling of 49.
            var met = verdicts.Count(v => v.Verdict == Verdict.Met);
            score = UnmetMustHaveCap * met / verdicts.Count;
        }

        // OrderBy is stable, so requirements of the same rank keep the order they were given in.
        var ordered = verdicts
            .OrderBy(v => v.Kind == RequirementKind.MustHave ? 0 : 1)
            .ThenBy(v => v.Verdict == Verdict.NotMet ? 0 : 1)
            .ToList();

        return new MatchResult(score, ordered);
    }

    private static RequirementVerdict Judge(string normalisedText, MatchRequirement requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement.Term))
        {
            throw new ArgumentException("A requirement term cannot be blank.", nameof(requirement));
        }

        var term = requirement.Term.Trim();
        var found = ContainsTerm(normalisedText, Normalise(term));
        var label = requirement.Kind == RequirementKind.MustHave ? "Must-have" : "Nice-to-have";

        return new RequirementVerdict(
            term,
            requirement.Kind,
            found ? Verdict.Met : Verdict.NotMet,
            found
                ? $"{label} \"{term}\" was found in the vacancy text."
                : $"{label} \"{term}\" was not found in the vacancy text.");
    }

    // Case-insensitive, and a term that starts or ends with a letter or digit must not be glued to another one,
    // so "Java" is not found in "JavaScript" and "SQL" is not found in "NoSQL".
    private static bool ContainsTerm(string text, string term)
    {
        var start = 0;

        while (start <= text.Length - term.Length)
        {
            var index = text.IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            var end = index + term.Length;
            var startsClean = !char.IsLetterOrDigit(term[0]) || index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            var endsClean = !char.IsLetterOrDigit(term[^1]) || end == text.Length || !char.IsLetterOrDigit(text[end]);

            if (startsClean && endsClean)
            {
                return true;
            }

            start = index + 1;
        }

        return false;
    }

    private static string Normalise(string value) => Whitespace().Replace(value, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

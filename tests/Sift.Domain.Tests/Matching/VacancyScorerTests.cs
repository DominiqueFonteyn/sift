using Sift.Domain.Matching;
using Sift.Domain.Requirements;

namespace Sift.Domain.Tests.Matching;

public sealed class VacancyScorerTests
{
    private static MatchRequirement Must(string term) => new(term, RequirementKind.MustHave);

    private static MatchRequirement Nice(string term) => new(term, RequirementKind.NiceToHave);

    public static TheoryData<string, MatchRequirement[], int> ExactScores => new()
    {
        { "We need C#", [Must("C#")], 100 },
        { "We need C#", [Must("C#"), Nice("Docker")], 50 },
        { "We need C# and Docker", [Must("C#"), Nice("Docker")], 100 },
        { "C# SQL Docker", [Must("C#"), Nice("SQL"), Nice("Docker"), Nice("Azure")], 83 },
        { "C# SQL", [Must("C#"), Nice("SQL"), Nice("Docker"), Nice("Azure")], 66 },
        { "C#", [Must("C#"), Nice("SQL"), Nice("Docker"), Nice("Azure")], 50 },
        { "Nothing relevant", [Must("C#"), Nice("SQL")], 0 },
        { "C# SQL Docker, on-site only", [Must("C#"), Must("Remote"), Nice("SQL"), Nice("Docker")], 36 },
        { "On-site only", [Must("C#"), Must("Remote"), Nice("SQL"), Nice("Docker")], 0 },
        { "Remote", [Must("C#"), Must("Remote")], 24 },
        { "Docker", [Nice("SQL"), Nice("Docker")], 75 },
        { "Nothing", [Nice("SQL"), Nice("Docker")], 50 },
    };

    [Theory]
    [MemberData(nameof(ExactScores))]
    public void Score_follows_the_rules(string vacancyText, MatchRequirement[] requirements, int expected)
    {
        var result = VacancyScorer.Score(vacancyText, requirements);

        Assert.Equal(expected, result.Score);
    }

    public static TheoryData<string, MatchRequirement[]> EveryMustHaveMet => new()
    {
        { "We need C#", [Must("C#"), Nice("Docker")] },
        { "C# and Java", [Must("C#"), Must("Java"), Nice("Docker"), Nice("SQL"), Nice("Azure")] },
        { "c#", [Must("C#"), Nice("A"), Nice("B"), Nice("C"), Nice("D"), Nice("E"), Nice("F"), Nice("G")] },
        { "C#", [Must("C#")] },
    };

    [Theory]
    [MemberData(nameof(EveryMustHaveMet))]
    public void Meeting_every_must_have_reaches_the_pass_threshold(string vacancyText, MatchRequirement[] requirements)
    {
        var result = VacancyScorer.Score(vacancyText, requirements);

        Assert.InRange(result.Score, VacancyScorer.PassThreshold, VacancyScorer.MaxScore);
    }

    public static TheoryData<string, MatchRequirement[]> AnUnmetMustHave => new()
    {
        { "C# SQL Docker, on-site only", [Must("C#"), Must("Remote"), Nice("SQL"), Nice("Docker")] },
        { "SQL Docker Azure Kubernetes Terraform Go Rust Python", [Must("C#"), Nice("SQL"), Nice("Docker"), Nice("Azure"), Nice("Kubernetes"), Nice("Terraform"), Nice("Go"), Nice("Rust"), Nice("Python")] },
        { "Remote and every nice-to-have, but no C", [Must("C#"), Must("Remote"), Must("English"), Nice("Remote2")] },
        { "Nothing", [Must("C#")] },
    };

    [Theory]
    [MemberData(nameof(AnUnmetMustHave))]
    public void An_unmet_must_have_caps_the_score_below_the_pass_threshold(string vacancyText, MatchRequirement[] requirements)
    {
        var result = VacancyScorer.Score(vacancyText, requirements);

        Assert.InRange(result.Score, 0, VacancyScorer.UnmetMustHaveCap);
    }

    [Fact]
    public void The_cap_holds_even_when_every_nice_to_have_is_met_and_most_must_haves_are()
    {
        var requirements = Enumerable.Range(1, 20).Select(i => Must($"must{i}"))
            .Concat(Enumerable.Range(1, 20).Select(i => Nice($"nice{i}")))
            .ToList();
        var text = string.Join(' ', requirements.Where(r => r.Term != "must20").Select(r => r.Term));

        var result = VacancyScorer.Score(text, requirements);

        Assert.True(result.Score <= VacancyScorer.UnmetMustHaveCap, $"Score was {result.Score}");
    }

    public static TheoryData<string, MatchRequirement[], string, MatchRequirement[]> MoreNiceToHavesScoreHigher => new()
    {
        {
            "C# SQL Docker", [Must("C#"), Nice("SQL"), Nice("Docker"), Nice("Azure")],
            "C# SQL", [Must("C#"), Nice("SQL"), Nice("Docker"), Nice("Azure")]
        },
        {
            "C# Azure", [Must("C#"), Nice("SQL"), Nice("Docker"), Nice("Azure")],
            "C#", [Must("C#"), Nice("SQL"), Nice("Docker"), Nice("Azure")]
        },
    };

    [Theory]
    [MemberData(nameof(MoreNiceToHavesScoreHigher))]
    public void Meeting_more_nice_to_haves_scores_higher(
        string moreText, MatchRequirement[] moreRequirements, string fewerText, MatchRequirement[] fewerRequirements)
    {
        var more = VacancyScorer.Score(moreText, moreRequirements);
        var fewer = VacancyScorer.Score(fewerText, fewerRequirements);

        Assert.True(more.Score > fewer.Score, $"{more.Score} should be greater than {fewer.Score}");
    }

    public static TheoryData<string, string> DifferentNiceToHavesInEqualNumbers => new()
    {
        { "C# SQL", "C# Docker" },
        { "C# SQL", "C# Azure" },
        { "C# Docker Azure", "C# SQL Docker" },
        { "C# SQL Docker Azure", "C# SQL Docker Azure" },
        { "C#", "C#" },
    };

    [Theory]
    [MemberData(nameof(DifferentNiceToHavesInEqualNumbers))]
    public void Nice_to_haves_count_equally_so_different_ones_matched_in_equal_numbers_score_the_same(string firstText, string secondText)
    {
        MatchRequirement[] requirements = [Must("C#"), Nice("SQL"), Nice("Docker"), Nice("Azure")];

        var first = VacancyScorer.Score(firstText, requirements);
        var second = VacancyScorer.Score(secondText, requirements);

        Assert.Equal(first.Score, second.Score);
    }

    [Fact]
    public void Nice_to_haves_count_equally_below_the_pass_threshold_too()
    {
        MatchRequirement[] requirements = [Must("C#"), Must("Remote"), Nice("SQL"), Nice("Docker")];

        var withSql = VacancyScorer.Score("SQL", requirements);
        var withDocker = VacancyScorer.Score("Docker", requirements);

        Assert.Equal(withSql.Score, withDocker.Score);
    }

    [Fact]
    public void Every_requirement_gets_exactly_one_verdict_and_an_explanation_naming_the_term()
    {
        MatchRequirement[] requirements = [Must("C#"), Nice("Docker")];

        var result = VacancyScorer.Score("We need C# and SQL", requirements);

        Assert.Equal(2, result.Verdicts.Count);
        var csharp = Assert.Single(result.Verdicts, v => v.Term == "C#");
        Assert.Equal(Verdict.Met, csharp.Verdict);
        Assert.Equal(RequirementKind.MustHave, csharp.Kind);
        Assert.Contains("\"C#\"", csharp.Explanation);
        Assert.Contains("was found", csharp.Explanation);
        Assert.DoesNotContain("not found", csharp.Explanation);
        var docker = Assert.Single(result.Verdicts, v => v.Term == "Docker");
        Assert.Equal(Verdict.NotMet, docker.Verdict);
        Assert.Equal(RequirementKind.NiceToHave, docker.Kind);
        Assert.Contains("\"Docker\"", docker.Explanation);
        Assert.Contains("was not found", docker.Explanation);
    }

    [Fact]
    public void Unmet_must_haves_are_listed_first()
    {
        MatchRequirement[] requirements = [Nice("SQL"), Must("C#"), Nice("Azure"), Nice("Docker"), Must("Remote")];

        var result = VacancyScorer.Score("C# SQL Docker, on-site only", requirements);

        Assert.Equal(["Remote", "C#", "Azure", "SQL", "Docker"], result.Verdicts.Select(v => v.Term).ToArray());
        Assert.Equal(RequirementKind.MustHave, result.Verdicts[0].Kind);
        Assert.Equal(Verdict.NotMet, result.Verdicts[0].Verdict);
    }

    [Theory]
    [InlineData("We need c# and sql", "C#", true)]
    [InlineData("We need C# and SQL", "sql", true)]
    [InlineData("We need JavaScript", "Java", false)]
    [InlineData("We use NoSQL", "SQL", false)]
    [InlineData("We use SQL.", "SQL", true)]
    [InlineData("(C#)", "C#", true)]
    [InlineData("Java, Kotlin", "Java", true)]
    [InlineData("Experience with Domain\n   Driven   Design", "Domain Driven Design", true)]
    [InlineData("ASP.NET", ".NET", true)]
    [InlineData("C++ and C#", "C++", true)]
    [InlineData("Embedded C development", "C#", false)]
    [InlineData("  Remote  ", "  Remote ", true)]
    public void A_term_is_found_case_insensitively_on_word_boundaries(string vacancyText, string term, bool expectedFound)
    {
        var result = VacancyScorer.Score(vacancyText, [Must(term)]);

        var verdict = Assert.Single(result.Verdicts);
        Assert.Equal(expectedFound ? Verdict.Met : Verdict.NotMet, verdict.Verdict);
    }

    [Fact]
    public void A_term_found_only_after_a_glued_occurrence_is_still_found()
    {
        var result = VacancyScorer.Score("JavaScript and Java", [Must("Java")]);

        Assert.Equal(Verdict.Met, Assert.Single(result.Verdicts).Verdict);
    }

    [Fact]
    public void Scoring_is_deterministic()
    {
        MatchRequirement[] requirements = [Must("C#"), Must("Remote"), Nice("SQL"), Nice("Docker")];
        const string text = "C# SQL Docker, on-site only";

        var first = VacancyScorer.Score(text, requirements);
        var second = VacancyScorer.Score(text, requirements);

        Assert.Equal(first.Score, second.Score);
        Assert.Equal(first.Verdicts, second.Verdicts);
    }

    [Fact]
    public void Scoring_without_requirements_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => VacancyScorer.Score("We need C#", []));
    }

    [Fact]
    public void A_blank_term_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => VacancyScorer.Score("We need C#", [Must("  ")]));
    }
}

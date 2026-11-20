using Sift.Domain.Vacancies;

namespace Sift.Domain.Tests.Vacancies;

public sealed class VacancyTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));


    public static TheoryData<string, string?, bool> FieldRules()
    {
        var data = new TheoryData<string, string?, bool>();

        foreach (var (field, max) in new[] { ("title", Vacancy.TitleMaxLength), ("company", Vacancy.CompanyMaxLength), ("rawText", Vacancy.RawTextMaxLength) })
        {
            data.Add(field, null, false);
            data.Add(field, "", false);
            data.Add(field, "   ", false);
            data.Add(field, "\t\n ", false);
            data.Add(field, "x", true);
            data.Add(field, new string('a', max), true);
            data.Add(field, new string('a', max + 1), false);
            // The contract limits the raw string, so surrounding whitespace counts too.
            data.Add(field, " " + new string('a', max), false);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(FieldRules))]
    public void Validates_each_field_as_the_contract_does(string field, string? value, bool expected)
    {
        var valid = Validate(field, value, out var error);

        Assert.Equal(expected, valid);
        Assert.Equal(expected, error is null);
    }

    [Fact]
    public void Creates_a_vacancy_trimming_title_and_company_but_keeping_the_text_exactly()
    {
        var ownerId = Guid.NewGuid();

        var vacancy = Vacancy.Create(Guid.NewGuid(), ownerId, "  Backend Engineer ", " Acme\t", "  We need C# and SQL\n", Now);

        Assert.Equal(ownerId, vacancy.OwnerId);
        Assert.Equal("Backend Engineer", vacancy.Title);
        Assert.Equal("Acme", vacancy.Company);
        Assert.Equal("  We need C# and SQL\n", vacancy.RawText);
        Assert.Equal(Now.ToUniversalTime(), vacancy.CreatedAt);
        Assert.Equal(vacancy.CreatedAt, vacancy.UpdatedAt);
    }

    [Theory]
    [InlineData(" ", "Acme", "text")]
    [InlineData("Title", " ", "text")]
    [InlineData("Title", "Acme", " ")]
    public void Refuses_to_create_an_invalid_vacancy(string title, string company, string rawText) =>
        Assert.Throws<ArgumentException>(() => Vacancy.Create(Guid.NewGuid(), Guid.NewGuid(), title, company, rawText, Now));

    [Fact]
    public void Requires_an_owner() =>
        Assert.Throws<ArgumentException>(() => Vacancy.Create(Guid.NewGuid(), Guid.Empty, "T", "C", "text", Now));

    [Fact]
    public void Requires_an_identifier() =>
        Assert.Throws<ArgumentException>(() => Vacancy.Create(Guid.Empty, Guid.NewGuid(), "T", "C", "text", Now));

    private static bool Validate(string field, string? value, out string? error) => field switch
    {
        "title" => Vacancy.TryValidateTitle(value, out error),
        "company" => Vacancy.TryValidateCompany(value, out error),
        "rawText" => Vacancy.TryValidateRawText(value, out error),
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
    };
}

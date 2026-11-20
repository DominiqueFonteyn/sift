using System.Globalization;

namespace Sift.Domain.Vacancies;

public sealed class Vacancy
{
    public const int TitleMaxLength = 200;

    public const int CompanyMaxLength = 200;

    // Counted in UTF-16 code units (string.Length), the same unit as JavaScript's .length on the client.
    public const int RawTextMaxLength = 20_000;

    // For persistence only.
    private Vacancy()
    {
        Title = null!;
        Company = null!;
        RawText = null!;
    }

    private Vacancy(Guid id, Guid ownerId, string title, string company, string rawText, DateTimeOffset createdAt)
    {
        Id = id;
        OwnerId = ownerId;
        Title = title;
        Company = company;
        RawText = rawText;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Title { get; private set; }

    public string Company { get; private set; }

    public string RawText { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static bool TryValidateTitle(string? title, out string? error)
    {
        error = string.IsNullOrWhiteSpace(title)
            ? "A vacancy needs a title."
            : title.Length > TitleMaxLength
                ? $"A vacancy title can be at most {TitleMaxLength} characters."
                : null;

        return error is null;
    }

    public static bool TryValidateCompany(string? company, out string? error)
    {
        error = string.IsNullOrWhiteSpace(company)
            ? "A vacancy needs a company."
            : company.Length > CompanyMaxLength
                ? $"A vacancy company can be at most {CompanyMaxLength} characters."
                : null;

        return error is null;
    }

    public static bool TryValidateRawText(string? rawText, out string? error)
    {
        error = string.IsNullOrWhiteSpace(rawText)
            ? "A vacancy needs its text."
            : rawText.Length > RawTextMaxLength
                ? $"The vacancy text can be at most {RawTextMaxLength.ToString("N0", CultureInfo.InvariantCulture)} characters."
                : null;

        return error is null;
    }

    public static Vacancy Create(Guid id, Guid ownerId, string title, string company, string rawText, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A vacancy needs an identifier.", nameof(id));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("A vacancy needs an owner.", nameof(ownerId));
        }

        if (!TryValidateTitle(title, out var titleError))
        {
            throw new ArgumentException(titleError, nameof(title));
        }

        if (!TryValidateCompany(company, out var companyError))
        {
            throw new ArgumentException(companyError, nameof(company));
        }

        if (!TryValidateRawText(rawText, out var rawTextError))
        {
            throw new ArgumentException(rawTextError, nameof(rawText));
        }

        return new Vacancy(id, ownerId, title.Trim(), company.Trim(), rawText, createdAt.ToUniversalTime());
    }
}

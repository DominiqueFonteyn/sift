using System.Globalization;

namespace Sift.Application.Common;

public sealed record PageRequest
{
    public const string PageField = "page";
    public const string PageSizeField = "pageSize";
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }

    public int PageSize { get; }

    // long because a page near int.MaxValue times the page size overflows int, and SQLite treats a negative offset as 0.
    public long Offset => (long)(Page - 1) * PageSize;

    // Takes the raw query values so a non-numeric value gets the same validation problem as an out-of-range one.
    public static bool TryCreate(
        string? page,
        string? pageSize,
        out PageRequest request,
        out IReadOnlyDictionary<string, string[]> errors)
    {
        var problems = new Dictionary<string, string[]>();

        var pageValue = Parse(page, DefaultPage, int.MaxValue, PageField, "Page must be a whole number of at least 1.", problems);
        var pageSizeValue = Parse(pageSize, DefaultPageSize, MaxPageSize, PageSizeField,
            $"Page size must be a whole number between 1 and {MaxPageSize}.", problems);

        request = new PageRequest(pageValue, pageSizeValue);
        errors = problems;

        return problems.Count == 0;
    }

    private static int Parse(string? raw, int fallback, int max, string field, string message, Dictionary<string, string[]> problems)
    {
        if (raw is null)
        {
            return fallback;
        }

        if (int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= 1 && value <= max)
        {
            return value;
        }

        problems[field] = [message];
        return fallback;
    }
}

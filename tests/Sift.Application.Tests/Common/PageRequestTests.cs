using Sift.Application.Common;

namespace Sift.Application.Tests.Common;

public sealed class PageRequestTests
{
    [Theory]
    [InlineData(null, null, 1, 20, 0)]
    [InlineData("1", "1", 1, 1, 0)]
    [InlineData("3", "20", 3, 20, 40)]
    [InlineData("2", "100", 2, 100, 100)]
    [InlineData("21474838", "100", 21474838, 100, 2147483700)]
    [InlineData("2147483647", "100", 2147483647, 100, 214748364600)]
    public void Accepts_valid_values_and_applies_defaults(string? page, string? pageSize, int expectedPage, int expectedPageSize, long expectedOffset)
    {
        var valid = PageRequest.TryCreate(page, pageSize, out var request, out var errors);

        Assert.True(valid);
        Assert.Empty(errors);
        Assert.Equal(expectedPage, request.Page);
        Assert.Equal(expectedPageSize, request.PageSize);
        Assert.Equal(expectedOffset, request.Offset);
    }

    [Theory]
    [InlineData("0", null, PageRequest.PageField)]
    [InlineData("-1", null, PageRequest.PageField)]
    [InlineData("abc", null, PageRequest.PageField)]
    [InlineData("1.5", null, PageRequest.PageField)]
    [InlineData("", null, PageRequest.PageField)]
    [InlineData("99999999999", null, PageRequest.PageField)]
    [InlineData(null, "0", PageRequest.PageSizeField)]
    [InlineData(null, "101", PageRequest.PageSizeField)]
    [InlineData(null, "ten", PageRequest.PageSizeField)]
    public void Rejects_out_of_range_or_non_numeric_values(string? page, string? pageSize, string field)
    {
        var valid = PageRequest.TryCreate(page, pageSize, out _, out var errors);

        Assert.False(valid);
        Assert.Equal(field, Assert.Single(errors).Key);
    }

    [Fact]
    public void Reports_both_fields_when_both_are_invalid()
    {
        var valid = PageRequest.TryCreate("0", "500", out _, out var errors);

        Assert.False(valid);
        Assert.Equal(
            new[] { PageRequest.PageField, PageRequest.PageSizeField }.Order(),
            errors.Keys.Order());
        Assert.Equal("Page size must be a whole number between 1 and 100.", Assert.Single(errors[PageRequest.PageSizeField]));
    }
}

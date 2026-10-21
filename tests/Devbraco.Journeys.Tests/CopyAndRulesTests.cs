using Devbraco.Journeys;

namespace Devbraco.Journeys.Tests;

public class CopyAndRulesTests
{
    [Theory]
    [InlineData(null, "Email address")]
    [InlineData("", "Email address")]
    [InlineData("   ", "Email address")]
    [InlineData("Work email", "Work email")]
    public void Blank_copy_inherits_the_code_default(string? cmsValue, string expected)
    {
        Assert.Equal(expected, CopyText.Resolve(cmsValue, "Email address"));
    }

    [Fact]
    public void Required_rejects_whitespace_only()
    {
        var rule = Rule.Required();
        Assert.False(rule.TryValidate("  ", out var message));
        Assert.False(string.IsNullOrWhiteSpace(message));
        Assert.True(rule.TryValidate("M-1", out _));
    }

    [Fact]
    public void Email_allows_empty_and_rejects_malformed()
    {
        var rule = Rule.Email();
        Assert.True(rule.TryValidate(null, out _));
        Assert.False(rule.TryValidate("not-an-email", out _));
        Assert.True(rule.TryValidate("person@example.com", out _));
    }

    [Fact]
    public void Min_age_rejects_a_recent_date()
    {
        var rule = Rule.MinAge(18);
        var recent = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10)).ToString("yyyy-MM-dd");
        var adult = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)).ToString("yyyy-MM-dd");
        Assert.False(rule.TryValidate(recent, out _));
        Assert.True(rule.TryValidate(adult, out _));
    }
}

namespace Domain.Tests;

using TodoSample.Domain;

public class TagTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryCreate_empty_or_null_fails(string? value)
    {
        var result = Tag.TryCreate(value);

        result.Should().BeFailure()
            .Which.Should().BeOfType<ValidationError>();
    }

    [Theory]
    [InlineData("work")]
    [InlineData("high-priority")]
    [InlineData("sprint-2026-q1")]
    public void TryCreate_valid_tag_succeeds(string value)
    {
        var result = Tag.TryCreate(value);

        result.Should().BeSuccess()
            .Which.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("Work")]
    [InlineData("HIGH-PRIORITY")]
    [InlineData("has spaces")]
    [InlineData("special!chars")]
    [InlineData("-leading-hyphen")]
    [InlineData("trailing-hyphen-")]
    public void TryCreate_invalid_format_fails(string value)
    {
        var result = Tag.TryCreate(value);

        result.Should().BeFailure();
    }

    [Fact]
    public void TryCreate_exceeding_50_chars_fails()
    {
        var longTag = new string('a', 51);
        var result = Tag.TryCreate(longTag);

        result.Should().BeFailure();
    }
}

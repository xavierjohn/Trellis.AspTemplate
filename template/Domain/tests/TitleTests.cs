namespace Domain.Tests;

using TodoSample.Domain;

public class TitleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryCreate_empty_or_null_fails(string? value)
    {
        var result = Title.TryCreate(value);

        result.Should().BeFailure()
            .Which.Should().BeOfType<ValidationError>();
    }

    [Fact]
    public void TryCreate_valid_title_succeeds()
    {
        var result = Title.TryCreate("Buy groceries");

        result.Should().BeSuccess()
            .Which.Value.Should().Be("Buy groceries");
    }

    [Fact]
    public void TryCreate_exceeding_200_chars_fails()
    {
        var longTitle = new string('a', 201);
        var result = Title.TryCreate(longTitle);

        result.Should().BeFailure()
            .Which.Should().BeOfType<ValidationError>();
    }

    [Fact]
    public void TryCreate_exactly_200_chars_succeeds()
    {
        var maxTitle = new string('a', 200);
        var result = Title.TryCreate(maxTitle);

        result.Should().BeSuccess();
    }
}

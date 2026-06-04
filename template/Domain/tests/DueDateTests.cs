namespace Domain.Tests;

using TodoSample.Domain;

public class DueDateTests
{
    [Fact]
    public void TryCreate_valid_date_succeeds()
    {
        var date = DateTime.UtcNow.AddDays(7);
        var result = DueDate.TryCreate(date);

        result.Should().BeSuccess()
            .Which.Value.Should().Be(date);
    }

    [Fact]
    public void TryCreate_min_value_fails()
    {
        var result = DueDate.TryCreate(DateTime.MinValue);

        result.Should().BeFailure()
            .Which.Should().BeOfType<Error.InvalidInput>();
    }
}

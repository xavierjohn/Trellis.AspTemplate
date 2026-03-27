namespace Domain.Tests;

using TodoSample.Domain;

public class TodoIdTests
{
    [Fact]
    public void NewUniqueV7_creates_valid_id()
    {
        var id = TodoId.NewUniqueV7();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void TryCreate_empty_guid_fails()
    {
        var result = TodoId.TryCreate(Guid.Empty);

        result.Should().BeFailure();
    }

    [Fact]
    public void TryCreate_valid_guid_succeeds()
    {
        var guid = Guid.NewGuid();
        var result = TodoId.TryCreate(guid);

        result.Should().BeSuccess()
            .Which.Value.Should().Be(guid);
    }
}

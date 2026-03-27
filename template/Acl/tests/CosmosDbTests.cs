namespace AntiCorruptionLayer.Tests;

using TodoSample.AntiCorruptionLayer;

public class CosmosDbTests
{
    [Theory]
    [InlineData("local")]
    [InlineData("test")]
    public void Will_get_CosmosDb_URL(string env)
    {
        // Arrange
        EnvironmentOptions environmentOptions = new()
        {
            Environment = env,
            RegionShortName = "usw2",
            ServiceName = "tdo"
        };

        var expected = $"{env}-tdo-cosno";

        // Act
        var actual = environmentOptions.GetCosmosDbNameShared();

        // Assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(CloudType.AzureCloud, "https://ppe-tdo-cosno.documents.azure.com:443/")]
    [InlineData(CloudType.AzureUSGovernment, "https://ppe-tdo-cosno.documents.azure.us:443/")]
    [InlineData(CloudType.AzureChinaCloud, "https://ppe-tdo-cosno.documents.azure.cn:443/")]
    [InlineData(CloudType.AzureGermanCloud, "https://ppe-tdo-cosno.documents.azure.com:443/")]
    public void Will_get_url_for_Cloud(string cloudType, string expectedUrl)
    {
        // Arrange
        EnvironmentOptions environmentOptions = new()
        {
            Environment = EnvironmentType.Ppe,
            RegionShortName = "usw2",
            ServiceName = "tdo",
            Cloud = cloudType
        };

        // Act
        var actualUrl = environmentOptions.GetCosmosDbNameSharedUrl();

        // Assert
        actualUrl.Should().Be(expectedUrl);
    }
}

namespace AntiCorruptionLayer.Tests;

using TodoSample.AntiCorruptionLayer;

public class KeyVaultTests
{
    [Theory]
    [InlineData("local")]
    [InlineData("test")]
    public void Will_get_KeyVault_name(string env)
    {
        // Arrange
        EnvironmentOptions environmentOptions = new()
        {
            Environment = env,
            RegionShortName = "usw2",
            ServiceName = "bwf"
        };

        var expected = $"{env}-bwf-usw2-kv";

        // Act
        var actual = environmentOptions.GetKeyVaultName();

        // Assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(CloudType.AzureCloud, "https://ppe-bwf-usw2-kv.vault.azure.net/")]
    [InlineData(CloudType.AzureUSGovernment, "https://ppe-bwf-usw2-kv.vault.usgovcloudapi.net/")]
    [InlineData(CloudType.AzureChinaCloud, "https://ppe-bwf-usw2-kv.vault.azure.cn/")]
    [InlineData(CloudType.AzureGermanCloud, "https://ppe-bwf-usw2-kv.vault.microsoftazure.de/")]
    public void Will_get_keyvault_uri_for_Cloud(string cloudType, string expectedUri)
    {
        // Arrange
        EnvironmentOptions environmentOptions = new()
        {
            Environment = EnvironmentType.Ppe,
            RegionShortName = "usw2",
            ServiceName = "bwf",
            Cloud = cloudType
        };

        // Act
        var actualUri = environmentOptions.GetKeyVaultUri();

        // Assert
        actualUri.Should().Be(expectedUri);
    }
}

namespace AntiCorruptionLayer.Tests;

using TodoSample.AntiCorruptionLayer;
using Xunit;

public class StorageNameTests
{
    [Theory]
    [InlineData("local")]
    [InlineData("test")]
    public void Will_get_storage_name(string env)
    {
        // Arrange
        EnvironmentOptions environmentOptions = new()
        {
            Environment = env,
            RegionShortName = "usw2",
            ServiceName = "tdo"
        };

        var expected = $"{env}tdost";

        // Act
        var actual = environmentOptions.GetStorageNameShared();

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void Will_throw_exception_name_too_long()
    {
        // Arrange
        EnvironmentOptions environmentOptions = new()
        {
            Environment = "alongenvironmentthatdoesnotexist",
            RegionShortName = "usw3",
            ServiceName = "aservicenamewithasuperlongname"
        };

        // Act
        Action act = () => environmentOptions.GetStorageNameShared();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(CloudType.AzureCloud, "https://ppetdost.blob.core.windows.net")]
    [InlineData(CloudType.AzureUSGovernment, "https://ppetdost.blob.core.usgovcloudapi.net")]
    [InlineData(CloudType.AzureChinaCloud, "https://ppetdost.blob.core.chinacloud.cn")]
    [InlineData(CloudType.AzureGermanCloud, "https://ppetdost.blob.core.cloudapi.de")]
    public void Will_get_blob_url_for_Cloud(string cloudType, string expectedUrl)
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
        var actualUrl = environmentOptions.GetBlobStorageSharedUrl();

        // Assert
        actualUrl.Should().Be(expectedUrl);

    }
}

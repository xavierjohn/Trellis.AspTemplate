namespace AntiCorruptionLayer.Tests;

using TodoSample.AntiCorruptionLayer;

public class ServiceBusTests
{
    [Theory]
    [InlineData("local")]
    [InlineData("test")]
    public void Will_get_ServiceBus_name(string env)
    {
        // Arrange
        EnvironmentOptions environmentOptions = new()
        {
            Environment = env,
            RegionShortName = "usw2",
            ServiceName = "tdo"
        };

        var expected = $"{env}-tdo-sbns";

        // Act
        var actual = environmentOptions.GetServiceBusName();

        // Assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(CloudType.AzureCloud, "ppe-tdo-sbns.servicebus.windows.net")]
    [InlineData(CloudType.AzureUSGovernment, "ppe-tdo-sbns.servicebus.usgovcloudapi.net")]
    [InlineData(CloudType.AzureChinaCloud, "ppe-tdo-sbns.servicebus.chinacloudapi.cn")]
    [InlineData(CloudType.AzureGermanCloud, "ppe-tdo-sbns.servicebus.cloudapi.de")]
    public void Will_get_namespace_for_Cloud(string cloudType, string expectedNamespace)
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
        var actualNamespace = environmentOptions.GetServiceBusNamespace();

        // Assert
        actualNamespace.Should().Be(expectedNamespace);
    }
}

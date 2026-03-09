namespace AntiCorruptionLayer.Tests;


[Trait("Category", Constants.Integration)]
public class DontRunOnBuildServer
{
    [Fact]
    public void This_test_should_be_skipped_on_the_build_server()
    {
        // Arrange
        //TODO: Change this code to identify your build server.
        var isBuildServer = Environment.GetEnvironmentVariable("IS_BUILD_SERVER");

        // Act
        var isSet = !string.IsNullOrEmpty(isBuildServer);

        // Assert
        isSet.Should().BeFalse();
    }
}

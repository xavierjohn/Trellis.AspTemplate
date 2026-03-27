namespace TodoSample.AntiCorruptionLayer;

public static class EnvironmentOptionsServiceBusExt
{
    public static string GetServiceBusName(this EnvironmentOptions settings) => settings.GetSharedResourceName("sbns");

    public static string GetServiceBusNamespace(this EnvironmentOptions settings) =>
        settings.Cloud switch
        {
            CloudType.AzureCloud => $"{settings.GetServiceBusName()}.servicebus.windows.net",
            CloudType.AzureUSGovernment => $"{settings.GetServiceBusName()}.servicebus.usgovcloudapi.net",
            CloudType.AzureChinaCloud => $"{settings.GetServiceBusName()}.servicebus.chinacloudapi.cn",
            CloudType.AzureGermanCloud => $"{settings.GetServiceBusName()}.servicebus.cloudapi.de",
            _ => throw new NotSupportedException($"Cloud type '{settings.Cloud}' is not supported.")
        };
}

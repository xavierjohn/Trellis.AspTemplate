namespace TodoSample.AntiCorruptionLayer;

public static class EnvironmentOptionsCosmosDbExt
{
    public static string GetCosmosDbNameShared(this EnvironmentOptions settings) => settings.GetSharedResourceName("cosno");

    public static string GetCosmosDbNameSharedUrl(this EnvironmentOptions settings)
    {
        return settings.Cloud switch
        {
            CloudType.AzureCloud => $"https://{GetCosmosDbNameShared(settings)}.documents.azure.com:443/",
            CloudType.AzureUSGovernment => $"https://{GetCosmosDbNameShared(settings)}.documents.azure.us:443/",
            CloudType.AzureChinaCloud => $"https://{GetCosmosDbNameShared(settings)}.documents.azure.cn:443/",
            CloudType.AzureGermanCloud => $"https://{GetCosmosDbNameShared(settings)}.documents.azure.com:443/",
            _ => throw new NotSupportedException($"Cloud type '{settings.Cloud}' is not supported.")
        };
    }
}

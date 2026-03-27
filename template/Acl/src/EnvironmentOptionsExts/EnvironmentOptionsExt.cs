namespace TodoSample.AntiCorruptionLayer;

public static class EnvironmentOptionsExt
{
    // Region specific resources.
    // Example: App Service, Key Vault, Managed Identity. etc.
    public static string GetRegionalResourceName(this EnvironmentOptions settings, string resourceType) =>
        $"{settings.Environment}-{settings.ServiceName}-{settings.RegionShortName}-{resourceType}".ToLowerInvariant();

    // Shared resources.
    // Example: Storage Account, Cosmos DB, SQL etc.
    public static string GetSharedResourceName(this EnvironmentOptions settings, string resourceType) =>
        $"{settings.Environment}-{settings.ServiceName}-{resourceType}".ToLowerInvariant();
}

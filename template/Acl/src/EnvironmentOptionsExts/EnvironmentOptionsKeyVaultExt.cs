namespace TodoSample.AntiCorruptionLayer;

public static class EnvironmentOptionsKeyVaultExt
{
    public static string GetKeyVaultName(this EnvironmentOptions settings) => settings.GetRegionalResourceName("kv");

    public static string GetKeyVaultUri(this EnvironmentOptions settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var keyVaultName = settings.GetKeyVaultName();

        // Map cloud to the known Key Vault endpoint suffix
        var dnsSuffix = settings.Cloud switch
        {
            CloudType.AzureCloud => "vault.azure.net",
            CloudType.AzureUSGovernment => "vault.usgovcloudapi.net",
            CloudType.AzureChinaCloud => "vault.azure.cn",
            CloudType.AzureGermanCloud => "vault.microsoftazure.de",
            _ => throw new NotSupportedException($"Cloud type '{settings.Cloud}' is not supported for Key Vault URI generation.")
        };

        return $"https://{keyVaultName}.{dnsSuffix}/";
    }
}

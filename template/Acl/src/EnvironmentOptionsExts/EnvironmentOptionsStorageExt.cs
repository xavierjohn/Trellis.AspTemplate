namespace TodoSample.AntiCorruptionLayer;

using System.Text.RegularExpressions;

public static partial class EnvironmentOptionsStorageExt
{
    /// <summary>
    /// Generates a valid Azure Storage Account name from the environment options.
    /// </summary>
    /// <param name="settings">The environment options.</param>
    /// <returns>A sanitized storage account name.</returns>
    /// <exception cref="ArgumentException">Thrown if the resulting name is not between 3 and 24 characters.</exception>
    public static string GetStorageNameShared(this EnvironmentOptions settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var name = settings.GetSharedResourceName("st");
        name = GetOnlyLettersAndNumbers().Replace(name, "");

        if (name.Length < 3 || name.Length > 24)
            throw new ArgumentException("The storage name must be between 3 and 24 characters long.", nameof(settings));

        return name;
    }

    /// <summary>
    /// Gets the Blob Storage URL for the current environment.
    /// </summary>
    /// <param name="settings">The environment options.</param>
    /// <returns>The Blob Storage URL.</returns>
    /// <exception cref="ArgumentNullException">Thrown if settings is null.</exception>
    /// <exception cref="NotSupportedException">Thrown if the cloud type is not supported.</exception>
    public static string GetBlobStorageSharedUrl(this EnvironmentOptions settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings.Cloud switch
        {
            CloudType.AzureCloud => $"https://{GetStorageNameShared(settings)}.blob.core.windows.net",
            CloudType.AzureUSGovernment => $"https://{GetStorageNameShared(settings)}.blob.core.usgovcloudapi.net",
            CloudType.AzureChinaCloud => $"https://{GetStorageNameShared(settings)}.blob.core.chinacloud.cn",
            CloudType.AzureGermanCloud => $"https://{GetStorageNameShared(settings)}.blob.core.cloudapi.de",
            _ => throw new NotSupportedException(
                $"Cloud type '{settings.Cloud}' is not supported for Blob Storage URL generation.")
        };
    }

    [GeneratedRegex("[^a-z0-9]")]
    private static partial Regex GetOnlyLettersAndNumbers();
}

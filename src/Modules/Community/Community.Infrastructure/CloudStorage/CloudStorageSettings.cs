namespace KoiFengShuiSystem.Modules.Community.Infrastructure.CloudStorage
{
    /// <summary>
    /// Clean-spelled options for the cloud storage provider. Bound from the
    /// legacy <c>CloundSettings</c> configuration section so existing
    /// appsettings/user-secrets keys keep working without migration:
    ///
    /// CloundSettings:CloundName -> CloudName
    /// CloundSettings:CloundKey  -> ApiKey
    /// CloundSettings:CloundSecret -> ApiSecret
    /// </summary>
    public class CloudStorageSettings
    {
        public string CloudName { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public string ApiSecret { get; set; } = string.Empty;
    }
}

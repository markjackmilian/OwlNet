namespace OwlNet.Application.Common.Constants;

/// <summary>
/// Constants for OpenCode Server integration configuration and defaults.
/// </summary>
public static class OpenCodeConstants
{
    /// <summary>
    /// The <see cref="Interfaces.IAppSettingService"/> key for the OpenCode Server base URL.
    /// </summary>
    public const string ServerUrlSettingKey = "OpenCode:ServerUrl";

    /// <summary>
    /// The default server URL when no setting is saved.
    /// </summary>
    public const string DefaultServerUrl = "http://127.0.0.1:4096";

    /// <summary>
    /// Default HTTP timeout in seconds for standard OpenCode API calls.
    /// </summary>
    public const int DefaultTimeoutSeconds = 30;

    /// <summary>
    /// Default polling interval in seconds for health check monitoring.
    /// </summary>
    public const int DefaultPollingIntervalSeconds = 30;

    /// <summary>
    /// Extended HTTP timeout in seconds for AI prompt/message calls that may take significant time.
    /// </summary>
    public const int MessageTimeoutSeconds = 300;
}

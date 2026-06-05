using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SeerrProxy.Configuration;

/// <summary>
/// Default behavior for TV requests when the client does not provide seasons.
/// </summary>
public enum TvRequestBehavior
{
    /// <summary>
    /// Request season one when no client seasons are provided.
    /// </summary>
    FirstSeasonOnly,

    /// <summary>
    /// Request all seasons when no client seasons are provided.
    /// </summary>
    AllSeasons,

    /// <summary>
    /// Require clients to provide explicit seasons.
    /// </summary>
    ClientSpecifiedSeasons
}

/// <summary>
/// Seerr Proxy plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    private const int DefaultTimeoutSeconds = 30;
    private const int MaximumTimeoutSeconds = 300;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        Enabled = false;
        SeerrBaseUrl = string.Empty;
        SeerrApiKey = string.Empty;
        RequestTimeoutSeconds = DefaultTimeoutSeconds;
        DefaultTvRequestBehavior = TvRequestBehavior.FirstSeasonOnly;
    }

    /// <summary>
    /// Gets or sets a value indicating whether proxy endpoints are enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the Seerr/Jellyseerr base URL.
    /// </summary>
    public string SeerrBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the Seerr/Jellyseerr API key.
    /// </summary>
    public string SeerrApiKey { get; set; }

    /// <summary>
    /// Gets or sets the outbound Seerr request timeout in seconds.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the default TV request behavior.
    /// </summary>
    public TvRequestBehavior DefaultTvRequestBehavior { get; set; }

    /// <summary>
    /// Gets a value indicating whether the required Seerr connection settings are present.
    /// </summary>
    /// <returns><c>true</c> when the Seerr base URL and API key are configured.</returns>
    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(SeerrBaseUrl)
               && !string.IsNullOrWhiteSpace(SeerrApiKey);
    }

    /// <summary>
    /// Gets the configured timeout clamped to a safe range.
    /// </summary>
    /// <returns>The clamped timeout.</returns>
    public TimeSpan GetRequestTimeout()
    {
        var seconds = RequestTimeoutSeconds;
        if (seconds <= 0)
        {
            seconds = DefaultTimeoutSeconds;
        }

        return TimeSpan.FromSeconds(Math.Min(seconds, MaximumTimeoutSeconds));
    }
}

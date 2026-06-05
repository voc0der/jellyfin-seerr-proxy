namespace Jellyfin.Plugin.SeerrProxy.Models;

/// <summary>
/// Plugin status response.
/// </summary>
public sealed class SeerrProxyStatusResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the plugin is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether required settings are present.
    /// </summary>
    public bool Configured { get; set; }

    /// <summary>
    /// Gets or sets the authenticated Jellyfin user ID.
    /// </summary>
    public string? JellyfinUserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current Jellyfin user is linked in Seerr.
    /// </summary>
    public bool? Linked { get; set; }

    /// <summary>
    /// Gets or sets the linked Seerr user ID.
    /// </summary>
    public int? SeerrUserId { get; set; }

    /// <summary>
    /// Gets or sets the linked Seerr display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Seerr was reachable during the status check.
    /// </summary>
    public bool? SeerrReachable { get; set; }

    /// <summary>
    /// Gets or sets the optional Seerr mapping error.
    /// </summary>
    public string? MappingError { get; set; }
}

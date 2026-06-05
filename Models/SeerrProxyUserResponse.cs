namespace Jellyfin.Plugin.SeerrProxy.Models;

/// <summary>
/// Linked Seerr user projection returned to Jellyfin clients.
/// </summary>
public sealed class SeerrProxyUserResponse
{
    /// <summary>
    /// Gets or sets the authenticated Jellyfin user ID.
    /// </summary>
    public string? JellyfinUserId { get; set; }

    /// <summary>
    /// Gets or sets the linked Seerr user ID.
    /// </summary>
    public int? SeerrUserId { get; set; }

    /// <summary>
    /// Gets or sets the linked Seerr display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Jellyfin user is linked in Seerr.
    /// </summary>
    public bool Linked { get; set; }
}

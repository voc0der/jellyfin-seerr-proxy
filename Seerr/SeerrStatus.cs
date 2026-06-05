using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.SeerrProxy.Seerr;

/// <summary>
/// Seerr public status response.
/// </summary>
public sealed class SeerrStatus
{
    /// <summary>
    /// Gets or sets the Seerr version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the Seerr commit tag.
    /// </summary>
    [JsonPropertyName("commitTag")]
    public string? CommitTag { get; set; }
}

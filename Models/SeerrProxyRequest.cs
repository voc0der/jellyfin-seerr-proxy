using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.SeerrProxy.Models;

/// <summary>
/// Request body accepted by the proxy request endpoint.
/// </summary>
public sealed class SeerrProxyRequest
{
    /// <summary>
    /// Gets or sets the media type.
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Gets or sets the Seerr media ID.
    /// </summary>
    public int? MediaId { get; set; }

    /// <summary>
    /// Gets or sets the TMDB ID, normalized internally to Seerr mediaId.
    /// </summary>
    public int? TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the optional TVDB ID.
    /// </summary>
    public int? TvdbId { get; set; }

    /// <summary>
    /// Gets or sets the optional TV seasons array or Seerr's "all" value.
    /// </summary>
    public JsonElement? Seasons { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a 4K request.
    /// </summary>
    [JsonPropertyName("is4k")]
    public bool? Is4K { get; set; }

    /// <summary>
    /// Gets or sets the optional Radarr/Sonarr server ID.
    /// </summary>
    public int? ServerId { get; set; }

    /// <summary>
    /// Gets or sets the optional quality profile ID.
    /// </summary>
    public int? ProfileId { get; set; }

    /// <summary>
    /// Gets or sets the optional root folder.
    /// </summary>
    public string? RootFolder { get; set; }

    /// <summary>
    /// Gets or sets the optional language profile ID.
    /// </summary>
    public int? LanguageProfileId { get; set; }

    /// <summary>
    /// Gets or sets optional service tags.
    /// </summary>
    public IReadOnlyList<int>? Tags { get; set; }
}

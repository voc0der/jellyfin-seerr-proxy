using System.Net.Http;
using System.Text.Json.Nodes;
using Jellyfin.Plugin.SeerrProxy.Configuration;

namespace Jellyfin.Plugin.SeerrProxy.Seerr;

/// <summary>
/// Typed Seerr/Jellyseerr API client.
/// </summary>
public interface ISeerrClient
{
    /// <summary>
    /// Gets Seerr public status.
    /// </summary>
    /// <param name="configuration">Plugin configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Seerr status.</returns>
    Task<SeerrStatus> GetStatusAsync(PluginConfiguration configuration, CancellationToken cancellationToken);

    /// <summary>
    /// Validates the configured API key.
    /// </summary>
    /// <param name="configuration">Plugin configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ValidateApiKeyAsync(PluginConfiguration configuration, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a Seerr user by linked Jellyfin user ID.
    /// </summary>
    /// <param name="configuration">Plugin configuration.</param>
    /// <param name="jellyfinUserId">Normalized Jellyfin user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Seerr user.</returns>
    Task<SeerrUser> GetUserByJellyfinIdAsync(
        PluginConfiguration configuration,
        string jellyfinUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a Seerr media request as the supplied Seerr user.
    /// </summary>
    /// <param name="configuration">Plugin configuration.</param>
    /// <param name="seerrUserId">Linked Seerr user ID.</param>
    /// <param name="payload">Safe Seerr request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw Seerr API result.</returns>
    Task<SeerrApiResult> CreateRequestAsync(
        PluginConfiguration configuration,
        int seerrUserId,
        JsonObject payload,
        CancellationToken cancellationToken);

    /// <summary>
    /// Forwards an allowed Seerr API request as the supplied Seerr user.
    /// </summary>
    /// <param name="configuration">Plugin configuration.</param>
    /// <param name="seerrUserId">Linked Seerr user ID.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="relativePath">Relative Seerr API path, including query string if present.</param>
    /// <param name="payload">Optional JSON request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw Seerr API result.</returns>
    Task<SeerrApiResult> ForwardApiRequestAsync(
        PluginConfiguration configuration,
        int seerrUserId,
        HttpMethod method,
        string relativePath,
        JsonNode? payload,
        CancellationToken cancellationToken);
}

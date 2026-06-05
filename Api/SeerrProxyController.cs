using System.Globalization;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jellyfin.Plugin.SeerrProxy.Configuration;
using Jellyfin.Plugin.SeerrProxy.Models;
using Jellyfin.Plugin.SeerrProxy.Seerr;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SeerrProxy.Api;

/// <summary>
/// Authenticated Jellyfin-to-Seerr proxy endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("Plugins/SeerrProxy")]
[Produces(MediaTypeNames.Application.Json)]
public class SeerrProxyController : ControllerBase
{
    private readonly ISeerrClient _seerrClient;
    private readonly IAuthorizationContext _authorizationContext;
    private readonly ILogger<SeerrProxyController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeerrProxyController"/> class.
    /// </summary>
    /// <param name="seerrClient">Seerr API client.</param>
    /// <param name="authorizationContext">Jellyfin authorization context.</param>
    /// <param name="logger">Logger.</param>
    public SeerrProxyController(
        ISeerrClient seerrClient,
        IAuthorizationContext authorizationContext,
        ILogger<SeerrProxyController> logger)
    {
        _seerrClient = seerrClient;
        _authorizationContext = authorizationContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets plugin status for the current Jellyfin user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status response.</returns>
    [HttpGet("Status")]
    [ProducesResponseType(typeof(SeerrProxyStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SeerrProxyStatusResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var configuration = GetConfiguration();
        var jellyfinUser = await GetAuthenticatedJellyfinUser().ConfigureAwait(false);
        if (jellyfinUser is null)
        {
            return Unauthorized(Error(StatusCodes.Status401Unauthorized, "MissingJellyfinUser", "Authenticated request is not associated with a Jellyfin user."));
        }

        var response = new SeerrProxyStatusResponse
        {
            Enabled = configuration.Enabled,
            Configured = configuration.IsConfigured(),
            JellyfinUserId = jellyfinUser.UserId
        };

        if (!configuration.Enabled || !configuration.IsConfigured())
        {
            return Ok(response);
        }

        try
        {
            var seerrUser = await _seerrClient.GetUserByJellyfinIdAsync(configuration, jellyfinUser.UserId, cancellationToken)
                .ConfigureAwait(false);
            response.Linked = true;
            response.SeerrReachable = true;
            response.SeerrUserId = seerrUser.Id;
            response.DisplayName = seerrUser.GetSafeDisplayName();
        }
        catch (SeerrApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            response.Linked = false;
            response.SeerrReachable = true;
        }
        catch (SeerrConnectionException ex)
        {
            _logger.LogWarning(ex, "Unable to reach Seerr while checking status for Jellyfin user {JellyfinUserId}", jellyfinUser.UserId);
            response.SeerrReachable = false;
            response.MappingError = "Seerr is unreachable.";
        }
        catch (SeerrConfigurationException ex)
        {
            response.MappingError = ex.Message;
        }

        return Ok(response);
    }

    /// <summary>
    /// Resolves the current Jellyfin user to the linked Seerr user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Linked user response.</returns>
    [HttpGet("User")]
    [ProducesResponseType(typeof(SeerrProxyUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SeerrProxyUserResponse>> GetUser(CancellationToken cancellationToken)
    {
        var configuration = GetConfiguration();
        var disabledOrUnconfigured = EnsureEnabledAndConfigured(configuration);
        if (disabledOrUnconfigured is not null)
        {
            return disabledOrUnconfigured;
        }

        var jellyfinUser = await GetAuthenticatedJellyfinUser().ConfigureAwait(false);
        if (jellyfinUser is null)
        {
            return Unauthorized(Error(StatusCodes.Status401Unauthorized, "MissingJellyfinUser", "Authenticated request is not associated with a Jellyfin user."));
        }

        try
        {
            var seerrUser = await _seerrClient.GetUserByJellyfinIdAsync(configuration, jellyfinUser.UserId, cancellationToken)
                .ConfigureAwait(false);
            return Ok(ToUserResponse(jellyfinUser.UserId, seerrUser));
        }
        catch (Exception ex) when (HandleProxyException(ex, out var actionResult))
        {
            return actionResult;
        }
    }

    /// <summary>
    /// Creates a Seerr media request as the current Jellyfin user's linked Seerr user.
    /// </summary>
    /// <param name="request">Proxy request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Seerr request result.</returns>
    [HttpPost("Request")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRequest([FromBody] SeerrProxyRequest? request, CancellationToken cancellationToken)
    {
        var configuration = GetConfiguration();
        var disabledOrUnconfigured = EnsureEnabledAndConfigured(configuration);
        if (disabledOrUnconfigured is not null)
        {
            return disabledOrUnconfigured;
        }

        var jellyfinUser = await GetAuthenticatedJellyfinUser().ConfigureAwait(false);
        if (jellyfinUser is null)
        {
            return Unauthorized(Error(StatusCodes.Status401Unauthorized, "MissingJellyfinUser", "Authenticated request is not associated with a Jellyfin user."));
        }

        if (!TryBuildSeerrRequestPayload(request, configuration, out var payload, out var validationError))
        {
            return BadRequest(Error(StatusCodes.Status400BadRequest, "InvalidRequest", validationError ?? "Invalid request body."));
        }

        SeerrUser seerrUser;
        try
        {
            seerrUser = await _seerrClient.GetUserByJellyfinIdAsync(configuration, jellyfinUser.UserId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (HandleProxyException(ex, out var resolveError))
        {
            return resolveError;
        }

        _logger.LogInformation(
            "Submitting Seerr {MediaType} request for Jellyfin user {JellyfinUserId} as Seerr user {SeerrUserId}",
            payload["mediaType"]?.GetValue<string>(),
            jellyfinUser.UserId,
            seerrUser.Id);

        try
        {
            var result = await _seerrClient.CreateRequestAsync(configuration, seerrUser.Id, payload, cancellationToken)
                .ConfigureAwait(false);
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex) when (HandleProxyException(ex, out var actionResult, notFoundMeansUnlinked: false))
        {
            return actionResult;
        }
    }

    /// <summary>
    /// Tests Seerr reachability and the configured API key.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection test response.</returns>
    [HttpPost("Test")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(typeof(TestConnectionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TestConnectionResponse>> TestConnection(CancellationToken cancellationToken)
    {
        var configuration = GetConfiguration();
        if (!configuration.IsConfigured())
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                Error(StatusCodes.Status503ServiceUnavailable, "PluginNotConfigured", "Seerr base URL and API key must be configured first."));
        }

        try
        {
            var status = await _seerrClient.GetStatusAsync(configuration, cancellationToken).ConfigureAwait(false);
            await _seerrClient.ValidateApiKeyAsync(configuration, cancellationToken).ConfigureAwait(false);

            return Ok(new TestConnectionResponse
            {
                Reachable = true,
                Authenticated = true,
                Version = status.Version,
                Message = "Successfully connected to Seerr."
            });
        }
        catch (Exception ex) when (HandleProxyException(ex, out var actionResult, notFoundMeansUnlinked: false))
        {
            return actionResult;
        }
    }

    private static PluginConfiguration GetConfiguration()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
    }

    private ObjectResult? EnsureEnabledAndConfigured(PluginConfiguration configuration)
    {
        if (!configuration.Enabled)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                Error(StatusCodes.Status403Forbidden, "PluginDisabled", "Seerr Proxy is disabled."));
        }

        if (!configuration.IsConfigured())
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                Error(StatusCodes.Status503ServiceUnavailable, "PluginNotConfigured", "Seerr base URL and API key must be configured first."));
        }

        return null;
    }

    private async Task<AuthenticatedJellyfinUser?> GetAuthenticatedJellyfinUser()
    {
        var authorizationInfo = await _authorizationContext.GetAuthorizationInfo(HttpContext).ConfigureAwait(false);
        if (authorizationInfo.UserId.Equals(Guid.Empty))
        {
            return null;
        }

        return new AuthenticatedJellyfinUser(
            authorizationInfo.UserId.ToString("N", CultureInfo.InvariantCulture),
            authorizationInfo.User?.Username);
    }

    private bool TryBuildSeerrRequestPayload(
        SeerrProxyRequest? request,
        PluginConfiguration configuration,
        out JsonObject payload,
        out string? error)
    {
        payload = [];
        error = null;

        if (request is null)
        {
            error = "Request body is required.";
            return false;
        }

        var mediaType = request.MediaType?.Trim().ToLowerInvariant();
        if (mediaType is not ("movie" or "tv"))
        {
            error = "mediaType must be either 'movie' or 'tv'.";
            return false;
        }

        var mediaId = request.MediaId ?? request.TmdbId;
        if (mediaId is null or <= 0)
        {
            error = "mediaId or tmdbId must be a positive number.";
            return false;
        }

        payload["mediaType"] = mediaType;
        payload["mediaId"] = mediaId.Value;
        payload["is4k"] = request.Is4K ?? false;

        AddOptionalNumber(payload, "tvdbId", request.TvdbId);
        AddOptionalNumber(payload, "serverId", request.ServerId);
        AddOptionalNumber(payload, "profileId", request.ProfileId);
        AddOptionalNumber(payload, "languageProfileId", request.LanguageProfileId);

        if (!string.IsNullOrWhiteSpace(request.RootFolder))
        {
            payload["rootFolder"] = request.RootFolder;
        }

        if (!TryAddTags(payload, request.Tags, out error))
        {
            return false;
        }

        if (mediaType == "tv" && !TryAddSeasons(payload, request.Seasons, configuration, out error))
        {
            return false;
        }

        return true;
    }

    private static void AddOptionalNumber(JsonObject payload, string name, int? value)
    {
        if (value.HasValue)
        {
            payload[name] = value.Value;
        }
    }

    private static bool TryAddTags(JsonObject payload, IReadOnlyList<int>? tags, out string? error)
    {
        error = null;
        if (tags is null)
        {
            return true;
        }

        var jsonTags = new JsonArray();
        foreach (var tag in tags)
        {
            if (tag < 0)
            {
                error = "tags must contain non-negative numeric IDs.";
                return false;
            }

            jsonTags.Add(tag);
        }

        payload["tags"] = jsonTags;
        return true;
    }

    private static bool TryAddSeasons(
        JsonObject payload,
        JsonElement? seasons,
        PluginConfiguration configuration,
        out string? error)
    {
        error = null;

        if (seasons.HasValue && seasons.Value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            return TryAddClientSeasons(payload, seasons.Value, out error);
        }

        switch (configuration.DefaultTvRequestBehavior)
        {
            case TvRequestBehavior.FirstSeasonOnly:
                var firstSeason = new JsonArray();
                firstSeason.Add(1);
                payload["seasons"] = firstSeason;
                return true;
            case TvRequestBehavior.AllSeasons:
                payload["seasons"] = "all";
                return true;
            case TvRequestBehavior.ClientSpecifiedSeasons:
                error = "seasons is required for TV requests.";
                return false;
            default:
                error = "Invalid default TV request behavior.";
                return false;
        }
    }

    private static bool TryAddClientSeasons(JsonObject payload, JsonElement seasons, out string? error)
    {
        error = null;
        if (seasons.ValueKind == JsonValueKind.String
            && string.Equals(seasons.GetString(), "all", StringComparison.OrdinalIgnoreCase))
        {
            payload["seasons"] = "all";
            return true;
        }

        if (seasons.ValueKind != JsonValueKind.Array)
        {
            error = "seasons must be an array of season numbers or the string 'all'.";
            return false;
        }

        var seen = new HashSet<int>();
        var jsonSeasons = new JsonArray();
        foreach (var season in seasons.EnumerateArray())
        {
            if (season.ValueKind != JsonValueKind.Number || !season.TryGetInt32(out var seasonNumber) || seasonNumber < 0)
            {
                error = "seasons must contain non-negative whole numbers.";
                return false;
            }

            if (seen.Add(seasonNumber))
            {
                jsonSeasons.Add(seasonNumber);
            }
        }

        if (jsonSeasons.Count == 0)
        {
            error = "seasons must contain at least one season number.";
            return false;
        }

        payload["seasons"] = jsonSeasons;
        return true;
    }

    private static SeerrProxyUserResponse ToUserResponse(string jellyfinUserId, SeerrUser seerrUser)
    {
        return new SeerrProxyUserResponse
        {
            JellyfinUserId = jellyfinUserId,
            SeerrUserId = seerrUser.Id,
            DisplayName = seerrUser.GetSafeDisplayName(),
            Linked = true
        };
    }

    private bool HandleProxyException(Exception exception, out ObjectResult actionResult, bool notFoundMeansUnlinked = true)
    {
        switch (exception)
        {
            case SeerrApiException seerrApiException when notFoundMeansUnlinked && seerrApiException.StatusCode == HttpStatusCode.NotFound:
                actionResult = StatusCode(
                    StatusCodes.Status404NotFound,
                    Error(StatusCodes.Status404NotFound, "SeerrUserNotLinked", "The authenticated Jellyfin user is not linked or imported in Seerr."));
                return true;
            case SeerrApiException seerrApiException:
                actionResult = StatusCode(
                    ToClientStatusCode(seerrApiException.StatusCode),
                    Error(ToClientStatusCode(seerrApiException.StatusCode), "SeerrError", seerrApiException.Message));
                return true;
            case SeerrConnectionException seerrConnectionException:
                _logger.LogWarning(seerrConnectionException, "Unable to reach Seerr");
                actionResult = StatusCode(
                    StatusCodes.Status502BadGateway,
                    Error(StatusCodes.Status502BadGateway, "SeerrUnreachable", "Seerr is unreachable."));
                return true;
            case SeerrConfigurationException seerrConfigurationException:
                actionResult = StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    Error(StatusCodes.Status503ServiceUnavailable, "PluginNotConfigured", seerrConfigurationException.Message));
                return true;
            default:
                actionResult = StatusCode(
                    StatusCodes.Status500InternalServerError,
                    Error(StatusCodes.Status500InternalServerError, "UnexpectedError", "Unexpected proxy error."));
                return false;
        }
    }

    private static int ToClientStatusCode(HttpStatusCode upstreamStatusCode)
    {
        var statusCode = (int)upstreamStatusCode;
        return statusCode is >= 400 and < 500 ? statusCode : StatusCodes.Status502BadGateway;
    }

    private static ErrorResponse Error(int statusCode, string error, string message)
    {
        return new ErrorResponse(statusCode, error, message);
    }

    private sealed record AuthenticatedJellyfinUser(string UserId, string? DisplayName);
}

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jellyfin.Plugin.SeerrProxy.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SeerrProxy.Seerr;

/// <inheritdoc />
public sealed class SeerrClient : ISeerrClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<SeerrClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeerrClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client supplied by dependency injection.</param>
    /// <param name="logger">Logger.</param>
    public SeerrClient(HttpClient httpClient, ILogger<SeerrClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SeerrStatus> GetStatusAsync(PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(configuration, "status"));
        var result = await SendAsync(configuration, request, cancellationToken).ConfigureAwait(false);

        return Deserialize<SeerrStatus>(result.BodyText) ?? new SeerrStatus();
    }

    /// <inheritdoc />
    public async Task ValidateApiKeyAsync(PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(configuration, "auth/me"));
        AddApiKey(request, configuration);

        await SendAsync(configuration, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SeerrUser> GetUserByJellyfinIdAsync(
        PluginConfiguration configuration,
        string jellyfinUserId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildUri(configuration, "user/jellyfin/" + Uri.EscapeDataString(jellyfinUserId)));
        AddApiKey(request, configuration);

        var result = await SendAsync(configuration, request, cancellationToken).ConfigureAwait(false);
        var user = Deserialize<SeerrUser>(result.BodyText);

        if (user is null || user.Id <= 0)
        {
            throw new SeerrApiException(HttpStatusCode.BadGateway, "Seerr returned an invalid user response.");
        }

        return user;
    }

    /// <inheritdoc />
    public async Task<SeerrApiResult> CreateRequestAsync(
        PluginConfiguration configuration,
        int seerrUserId,
        JsonObject payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(configuration, "request"))
        {
            Content = JsonContent.Create(payload)
        };
        AddApiKey(request, configuration);
        request.Headers.TryAddWithoutValidation("X-API-User", seerrUserId.ToString(CultureInfo.InvariantCulture));

        var result = await SendAsync(configuration, request, cancellationToken).ConfigureAwait(false);
        return new SeerrApiResult(result.StatusCode, ParseJson(result.BodyText));
    }

    private async Task<SeerrTransportResult> SendAsync(
        PluginConfiguration configuration,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutTokenSource.CancelAfter(configuration.GetRequestTimeout());

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutTokenSource.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SeerrConnectionException("Seerr request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new SeerrConnectionException("Seerr is unreachable.", ex);
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(timeoutTokenSource.Token)
            .ConfigureAwait(false);
        using var reader = new StreamReader(responseStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(timeoutTokenSource.Token).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return new SeerrTransportResult((int)response.StatusCode, body);
        }

        var message = ExtractErrorMessage(body)
                      ?? string.Create(
                          CultureInfo.InvariantCulture,
                          $"Seerr returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");

        _logger.LogWarning(
            "Seerr API returned HTTP {StatusCode} for {Method} {Path}",
            (int)response.StatusCode,
            request.Method,
            request.RequestUri?.AbsolutePath);

        throw new SeerrApiException(response.StatusCode, Sanitize(message, configuration.SeerrApiKey));
    }

    private static void AddApiKey(HttpRequestMessage request, PluginConfiguration configuration)
    {
        request.Headers.TryAddWithoutValidation("X-Api-Key", configuration.SeerrApiKey);
    }

    private static Uri BuildUri(PluginConfiguration configuration, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(configuration.SeerrBaseUrl))
        {
            throw new SeerrConfigurationException("Seerr base URL is not configured.");
        }

        var trimmedBaseUrl = configuration.SeerrBaseUrl.Trim().TrimEnd('/') + "/";
        if (!Uri.TryCreate(trimmedBaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new SeerrConfigurationException("Seerr base URL must be an absolute HTTP or HTTPS URL.");
        }

        var builder = new UriBuilder(baseUri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };

        var basePath = builder.Path.TrimEnd('/');
        if (!basePath.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase)
            && !basePath.Equals("api/v1", StringComparison.OrdinalIgnoreCase))
        {
            builder.Path = string.IsNullOrWhiteSpace(basePath) || basePath == "/"
                ? "api/v1/"
                : basePath.TrimStart('/') + "/api/v1/";
        }
        else
        {
            builder.Path = basePath.TrimStart('/') + "/";
        }

        return new Uri(builder.Uri, relativePath.TrimStart('/'));
    }

    private static T? Deserialize<T>(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    private static JsonNode? ParseJson(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return JsonValue.Create(body);
        }
    }

    private static string? ExtractErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            var node = JsonNode.Parse(body);
            if (node is JsonObject obj)
            {
                var message = GetString(obj, "message");
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }

                var error = GetString(obj, "error");
                if (!string.IsNullOrWhiteSpace(error))
                {
                    return error;
                }
            }
        }
        catch (JsonException)
        {
            return body.Length > 500 ? body[..500] : body;
        }

        return null;
    }

    private static string? GetString(JsonObject obj, string propertyName)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return null;
        }

        return node.GetValueKind() == JsonValueKind.String ? node.GetValue<string>() : node.ToJsonString();
    }

    private static string Sanitize(string value, string apiKey)
    {
        return string.IsNullOrEmpty(apiKey)
            ? value
            : value.Replace(apiKey, "[redacted]", StringComparison.Ordinal);
    }

    private sealed record SeerrTransportResult(int StatusCode, string BodyText);
}

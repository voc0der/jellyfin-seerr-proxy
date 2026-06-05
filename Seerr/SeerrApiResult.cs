using System.Text.Json.Nodes;

namespace Jellyfin.Plugin.SeerrProxy.Seerr;

/// <summary>
/// Raw Seerr API response.
/// </summary>
public sealed class SeerrApiResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeerrApiResult"/> class.
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="body">JSON response body.</param>
    public SeerrApiResult(int statusCode, JsonNode? body)
    {
        StatusCode = statusCode;
        Body = body;
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the JSON response body.
    /// </summary>
    public JsonNode? Body { get; }
}

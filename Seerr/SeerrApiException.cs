using System.Net;

namespace Jellyfin.Plugin.SeerrProxy.Seerr;

/// <summary>
/// Exception representing a non-success response from Seerr.
/// </summary>
public sealed class SeerrApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeerrApiException"/> class.
    /// </summary>
    /// <param name="statusCode">Upstream status code.</param>
    /// <param name="message">Sanitized error message.</param>
    public SeerrApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the upstream status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; }
}

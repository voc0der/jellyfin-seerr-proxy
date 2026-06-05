namespace Jellyfin.Plugin.SeerrProxy.Models;

/// <summary>
/// JSON error response returned by plugin endpoints.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorResponse"/> class.
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="error">Stable error identifier.</param>
    /// <param name="message">Displayable error message.</param>
    public ErrorResponse(int statusCode, string error, string message)
    {
        StatusCode = statusCode;
        Error = error;
        Message = message;
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets a stable error identifier.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Gets a displayable error message.
    /// </summary>
    public string Message { get; }
}

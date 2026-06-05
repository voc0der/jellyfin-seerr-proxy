namespace Jellyfin.Plugin.SeerrProxy.Seerr;

/// <summary>
/// Exception representing failure to reach Seerr.
/// </summary>
public sealed class SeerrConnectionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeerrConnectionException"/> class.
    /// </summary>
    /// <param name="message">Connection error message.</param>
    /// <param name="innerException">Original exception.</param>
    public SeerrConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

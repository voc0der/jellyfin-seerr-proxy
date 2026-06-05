namespace Jellyfin.Plugin.SeerrProxy.Seerr;

/// <summary>
/// Exception representing invalid plugin configuration.
/// </summary>
public sealed class SeerrConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeerrConfigurationException"/> class.
    /// </summary>
    /// <param name="message">Configuration error message.</param>
    public SeerrConfigurationException(string message)
        : base(message)
    {
    }
}

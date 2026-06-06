using Jellyfin.Plugin.SeerrProxy.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.SeerrProxy;

/// <summary>
/// Main Seerr Proxy plugin entry point.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Plugin unique identifier.
    /// </summary>
    public static readonly Guid PluginId = Guid.Parse("1ac3cf0f-f0f9-443a-be08-be38e48ff683");

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Jellyfin application paths.</param>
    /// <param name="xmlSerializer">Jellyfin XML serializer.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Seerr Proxy";

    /// <inheritdoc />
    public override Guid Id => PluginId;

    /// <inheritdoc />
    public override string Description =>
        "Authenticated Jellyfin transport for Seerr. Linked Jellyfin users can discover this plugin and call allowlisted Seerr API endpoints through Jellyfin without receiving Seerr credentials.";

    /// <inheritdoc />
    public override string ImageUrl =>
        "https://raw.githubusercontent.com/voc0der/jellyfin-seerr-proxy/main/icon.png";

    /// <summary>
    /// Gets the active plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
            }
        };
    }
}

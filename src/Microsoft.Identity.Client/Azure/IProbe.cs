using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Azure
{
    /// <summary>
    /// IProbe provides an interface for describing an entity that discovers environmental capabilities which enable
    /// a consumer to build a credential provider to assist authenticating to Azure
    /// </summary>
    public interface IProbe
    {
        /// <summary>
        /// Check if the probe is available for use in the current environment
        /// </summary>
        /// <returns>True if a credential provider can be built</returns>
        Task<bool> AvailableAsync();

        /// <summary>
        /// Create a credential provider from the information discovered by the probe
        /// </summary>
        /// <returns>A credential provider instance</returns>
        Task<ITokenProvider> ProviderAsync();
    }
}
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Client.Azure
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME

    /// <summary>
    /// SharedTokenCacheProbe (wip) provides shared access to tokens from the Microsoft family of products.
    /// This probe will provided access to tokens from accounts that have been authenticated in other Microsoft products to provide a single sign-on experience.
    /// </summary>
    public class SharedTokenCacheProbe : IProbe
    {
        /// <summary>
        ///     Check if the probe is available for use in the current environment
        /// </summary>
        /// <returns>True if a credential provider can be built</returns>
        public Task<bool> AvailableAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///     Create a managed identity credential provider from the information discovered by the probe
        /// </summary>
        /// <returns>A managed identity credential provider instance</returns>
        public Task<ITokenProvider> ProviderAsync()
        {
            throw new System.NotImplementedException();
        }
    }

#endif
}
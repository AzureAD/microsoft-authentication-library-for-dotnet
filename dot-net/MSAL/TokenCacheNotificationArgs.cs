using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSAL
{
    /// <summary>
    /// Contains parameters used by the ADAL call accessing the cache.
    /// </summary>
    public sealed class TokenCacheNotificationArgs
    {
        /// <summary>
        /// Gets the TokenCache
        /// </summary>
        public TokenCache TokenCache { get; internal set; }

        /// <summary>
        /// Gets the ClientId.
        /// </summary>
        public string ClientId { get; internal set; }

        /// <summary>
        /// Gets the Resource.
        /// </summary>
        public string Resource { get; internal set; }

        /// <summary>
        /// Gets the user's unique Id.
        /// </summary>
        public string UniqueId { get; internal set; }

        /// <summary>
        /// Gets the user's displayable Id.
        /// </summary>
        public string DisplayableId { get; internal set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSAL
{
    /// <summary>
    /// Token cache item. This is a readonly class that is instantiated internally by ADAL.
    /// </summary>
    public sealed class TokenCacheItem
    {
        /// <summary>
        /// Gets the Authority.
        /// </summary>
        public string Authority { get; private set; }

        /// <summary>
        /// Gets the ClientId.
        /// </summary>
        public string ClientId { get; internal set; }

        /// <summary>
        /// Gets the Expiration.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; internal set; }

        /// <summary>
        /// Gets the FamilyName.
        /// </summary>
        public string FamilyName { get; internal set; }

        /// <summary>
        /// Gets the GivenName.
        /// </summary>
        public string GivenName { get; internal set; }

        /// <summary>
        /// Gets the IdentityProviderName.
        /// </summary>
        public string IdentityProvider { get; internal set; }

        /// <summary>
        /// Gets the Resource.
        /// </summary>
        public string Resource { get; internal set; }

        /// <summary>
        /// Gets the TenantId.
        /// </summary>
        public string TenantId { get; internal set; }

        /// <summary>
        /// Gets the user's unique Id.
        /// </summary>
        public string UniqueId { get; internal set; }

        /// <summary>
        /// Gets the user's displayable Id.
        /// </summary>
        public string DisplayableId { get; internal set; }

        /// <summary>
        /// Gets the Access Token requested.
        /// </summary>
        public string AccessToken { get; internal set; }

        /// <summary>
        /// Gets the entire Id Token if returned by the service or null if no Id Token is returned.
        /// </summary>
        public string IdToken { get; internal set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    public sealed partial class PublicClientApplication : ClientApplicationBase
    {
        
        /// <summary>
        /// </summary>
        public PublicClientApplication(string clientId, string authority, TokenCache userTokenCache) : base(authority, clientId, DEFAULT_REDIRECT_URI, true)
        {
            this.UserTokenCache = userTokenCache;
        }
    }
}

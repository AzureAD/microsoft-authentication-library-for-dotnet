
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Core.Cache
{
    class MsalIdTokenCacheKey : MsalCredentialCacheKey
    {
        internal MsalIdTokenCacheKey(string environment, string tenantId, string userIdentifier, string clientId)
            : base(environment, tenantId, userIdentifier, CredentialType.idtoken, clientId, null)
        {
        }
    }
}

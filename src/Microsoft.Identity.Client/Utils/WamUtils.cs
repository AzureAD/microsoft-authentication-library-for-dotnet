// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_WAM

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Utils
{
    internal static class WamUtils
    {
        public static async Task<WebAccountProvider> FindAccountProviderForAuthorityAsync(
            IServiceBundle serviceBundle,
            AuthorityInfo authorityOverride)
        {
            var authority = authorityOverride == null
                ? Authority.CreateAuthority(serviceBundle)
                : Authority.CreateAuthorityWithOverride(serviceBundle, authorityOverride);

            // TODO(WAM):
            // How to handle to common authority applies to V1 semantics only.
            // It might be ok for now since we are prototyping but handling the V2 common authority will need to be done
            // through account control.
            // This is one of the most difficult aspects on MSAL <->WAM integration.        
            WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                "https://login.microsoft.com",
                authority.AuthorityInfo.CanonicalAuthority);
            return provider;
        }

        public static IAccount CreateMsalAccountFromWebAccount(WebAccount webAccount)
        {
            // TODO(WAM): what should environment be here?
            return new Account(webAccount.Id, webAccount.UserName, environment: string.Empty);
        }
    }
}

#endif // SUPPORTS_WAM

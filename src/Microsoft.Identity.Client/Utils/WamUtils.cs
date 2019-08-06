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

            // 
            // MSAL authority                                                                   WAM account provider
            // https://login.microsoftonline.com/common/v2.0                                    Show account control if no user object is present in the request. 
            //                                                                                  Looks at the user authority value in WAM webAccount object.
            // 
            // https://login.microsoftonline.com/consumers/v2.0                                 https://login.live.com - MSA
            // 
            // https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0      https://login.live.com - MSA
            // This GUID is the consumer tenant. You can think of MSA being a tenant of AAD
            // 
            // https://login.microsoftonline.com/contoso.onmicrosoft.com/v2.0                   https://login.windows.net - AAD
            // 
            // https://login.microsoftonline.com/organizations/v2.0                             https://login.windows.net - AAD
            //

            WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                "https://login.microsoft.com",
                authority.AuthorityInfo.CanonicalAuthority);
            return provider;
        }

        public static IAccount CreateMsalAccountFromWebAccount(WebAccount webAccount)
        {
            return new WamAccount(webAccount.Id, webAccount.UserName, environment: webAccount.WebAccountProvider.Authority);
        }
    }
}

#endif // SUPPORTS_WAM

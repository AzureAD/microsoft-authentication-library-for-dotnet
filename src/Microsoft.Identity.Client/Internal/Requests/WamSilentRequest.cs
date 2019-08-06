// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_WAM

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class WamSilentRequest : WamRequestBase
    {
        public WamSilentRequest(
            IServiceBundle serviceBundle, 
            AuthenticationRequestParameters authenticationRequestParameters, 
            IAcquireTokenParameters acquireTokenParameters) 
            : base(serviceBundle, authenticationRequestParameters, acquireTokenParameters)
        {
        }

        internal override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                "https://login.microsoft.com",
                AuthenticationRequestParameters.Account.Environment);

            WebAccount webAccount = await WebAuthenticationCoreManager.FindAccountAsync(provider, AuthenticationRequestParameters.Account.HomeAccountId.Identifier);

            // WebAccountProvider provider = await FindAccountProviderForAuthorityAsync(requestContext, commonParameters).ConfigureAwait(true);

            // TODO(wam): might need to have this method return the provider AND the webAccount since the MSAL account will need to cache the provider info for lookup
            // since we don't want to do HRD in ATS.
            // WebAccount webAccount = await GetWebAccountFromMsalAccountAsync(provider, silentParameters.Account).ConfigureAwait(true);

            WebTokenRequest request = CreateWebTokenRequest(provider);
            WebTokenRequestResult result = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(request, webAccount);

            return await HandleWebTokenRequestResultAsync(result).ConfigureAwait(false);
        }
    }
}

#endif // SUPPORTS_WAM

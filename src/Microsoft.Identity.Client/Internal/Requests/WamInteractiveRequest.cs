// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_WAM

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.UI.ApplicationSettings;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class WamInteractiveRequest : WamRequestBase
    {
        public WamInteractiveRequest(
            IServiceBundle serviceBundle, 
            AuthenticationRequestParameters authenticationRequestParameters, 
            IAcquireTokenParameters acquireTokenParameters) 
            : base(serviceBundle, authenticationRequestParameters, acquireTokenParameters)
        {
        }

        internal override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            WebAccountProviderCommand command = null;

            string tenantId = AuthenticationRequestParameters.Authority.GetTenantId();

            WebAccountProvider webAccountProvider = null;

            using (var wamAccountHandler = new WamAccountHandler())
            {
                if (string.Compare(tenantId, "common", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    command = await wamAccountHandler.ExecuteAsync().ConfigureAwait(true);
                    webAccountProvider = command.WebAccountProvider;
                }
                else if (string.Compare(tenantId, "consumers", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    webAccountProvider = await wamAccountHandler.GetMsaAccountProviderAsync().ConfigureAwait(true);
                }
                else
                {
                    // default to organizations since if it's not common (and the user can choose) and it's not consumers, then it's an AAD custom tenant
                    webAccountProvider = await wamAccountHandler.GetAadAccountProviderAsync().ConfigureAwait(true);
                }
            }

            WebTokenRequest request = CreateWebTokenRequest(webAccountProvider, forceAuthentication: true);

            WebTokenRequestResult result;

            //if (webAccount == null)
            {
                if (!string.IsNullOrWhiteSpace(AuthenticationRequestParameters.LoginHint))
                {
                    request.Properties["LoginHint"] = AuthenticationRequestParameters.LoginHint;
                }

                result = await WebAuthenticationCoreManager.RequestTokenAsync(request);
            }
            //else
            //{
            //    result = await WebAuthenticationCoreManager.RequestTokenAsync(request, webAccount);
            //}

            return await HandleWebTokenRequestResultAsync(result).ConfigureAwait(false);
        }
    }
}

#endif // SUPPORTS_WAM

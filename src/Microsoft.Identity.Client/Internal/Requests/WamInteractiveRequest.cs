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

            using (var wamAccountHandler = new WamAccountHandler())
            {
                command = await wamAccountHandler.ExecuteAsync().ConfigureAwait(true);
            }

            // WebAccountProvider provider = await FindAccountProviderForAuthorityAsync(requestContext, commonParameters).ConfigureAwait(true);
            //WebAccount webAccount = await GetWebAccountFromMsalAccountAsync(command.WebAccountProvider, interactiveParameters.Account).ConfigureAwait(true);
            WebTokenRequest request = CreateWebTokenRequest(command.WebAccountProvider, forceAuthentication: false);

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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal class WebAccountProviderFactory : IWebAccountProviderFactory
    {
        public async Task<WebAccountProvider> GetAccountProviderAsync(string authorityOrTenant)
        {
            WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                "https://login.microsoft.com",
               authorityOrTenant);

            return provider;
        }

        public async Task<WebAccountProvider> GetDefaultProviderAsync()
        {
            var provider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.windows.local");
            return provider;
        }

        public async Task<bool> IsDefaultAccountMsaAsync()
        {
            // provider for the "default" account
            var provider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.windows.local");
            return provider != null && string.Equals(Constants.ConsumerTenant, provider.Authority);
        }

        public bool IsConsumerProvider(WebAccountProvider webAccountProvider)
        {
            bool isConsumerTenant = string.Equals(webAccountProvider.Authority, "consumers", StringComparison.OrdinalIgnoreCase);
            return isConsumerTenant;

        }
    }
}

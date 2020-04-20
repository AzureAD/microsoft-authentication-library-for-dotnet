// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.Instance
{
    internal class AuthorityEndpoints
    {
        public AuthorityEndpoints(string authorizationEndpoint, string tokenEndpoint, string selfSignedJwtAudience)
        {
            AuthorizationEndpoint = authorizationEndpoint;
            TokenEndpoint = tokenEndpoint;
            SelfSignedJwtAudience = selfSignedJwtAudience;
        }

        public string AuthorizationEndpoint { get; }
        public string TokenEndpoint { get; }
        public string SelfSignedJwtAudience { get; }

        public static async Task UpdateAuthorityEndpointsAsync(
            AuthenticationRequestParameters requestParameters)
        {
            // This will make a network call unless instance discovery is cached, but this ok
            // GetAccounts and AcquireTokenSilent do not need this
            await UpdateAuthorityWithPreferredNetworkHostAsync(requestParameters).ConfigureAwait(false);

            requestParameters.Endpoints = await 
                requestParameters.RequestContext.ServiceBundle.AuthorityEndpointResolutionManager.ResolveEndpointsAsync(
                    requestParameters.AuthorityInfo,
                    requestParameters.LoginHint,
                    requestParameters.RequestContext).ConfigureAwait(false);
        }

        private async static Task UpdateAuthorityWithPreferredNetworkHostAsync(AuthenticationRequestParameters requestParameters)
        {
            InstanceDiscoveryMetadataEntry metadata = await
                requestParameters.RequestContext.ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryAsync(
                    requestParameters.AuthorityInfo.CanonicalAuthority,
                    requestParameters.RequestContext)
                .ConfigureAwait(false);

            requestParameters.Authority = Authority.CreateAuthorityWithEnvironment(
                    requestParameters.AuthorityInfo,
                    metadata.PreferredNetwork);
        }
    }
}

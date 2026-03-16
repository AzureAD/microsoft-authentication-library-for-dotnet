// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal class SecretStringClientCredential : IClientCredential
    {
        internal string Secret { get; }

        public AssertionType AssertionType => AssertionType.Secret;

        public SecretStringClientCredential(string secret)
        {
            Secret = secret;
        }

        public Task<ClientCredentialApplicationResult> AddConfidentialClientParametersAsync(
            OAuth2Client oAuth2Client,
            AuthenticationRequestParameters requestParameters,
            ICryptographyManager cryptographyManager, 
            string tokenEndpoint, 
            CancellationToken cancellationToken)
        {
            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientSecret, Secret);
            return Task.FromResult(ClientCredentialApplicationResult.None);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal class SignedAssertionDelegateClientCredential : IClientCredential
    {
        internal Func<CancellationToken, Task<string>> _signedAssertionDelegate { get; }

        public SignedAssertionDelegateClientCredential(Func<CancellationToken, Task<string>> signedAssertionDelegate)
        {
            _signedAssertionDelegate = signedAssertionDelegate;
        }

        public async Task AddConfidentialClientParametersAsync(OAuth2Client oAuth2Client, ILoggerAdapter logger, ICryptographyManager cryptographyManager, string clientId, string tokenEndpoint, bool sendX5C, CancellationToken cancellationToken)
        {
            string signedAssertion = await _signedAssertionDelegate(cancellationToken).ConfigureAwait(false);

            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);
            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, signedAssertion);
        }
    }
}

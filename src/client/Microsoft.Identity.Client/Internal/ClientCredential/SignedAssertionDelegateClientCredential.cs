﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal class SignedAssertionDelegateClientCredential : IClientCredential
    {
        internal Func<CancellationToken, Task<string>> _signedAssertionDelegate { get; }
        internal Func<AssertionRequestOptions, Task<string>> _signedAssertionWithInfoDelegate { get; }
        public AssertionType AssertionType => AssertionType.ClientAssertion;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public SignedAssertionDelegateClientCredential(Func<CancellationToken, Task<string>> signedAssertionDelegate)
        {
            _signedAssertionDelegate = signedAssertionDelegate;
        }

        public SignedAssertionDelegateClientCredential(Func<AssertionRequestOptions, Task<string>> signedAssertionDelegate)
        {
            _signedAssertionWithInfoDelegate = signedAssertionDelegate;
        }

        public async Task AddConfidentialClientParametersAsync(
            OAuth2Client oAuth2Client,
            AuthenticationRequestParameters requestParameters,
            ICryptographyManager cryptographyManager,
            string tokenEndpoint,
            CancellationToken cancellationToken)
        {
            string signedAssertion = await (_signedAssertionDelegate != null 
                ? _signedAssertionDelegate(cancellationToken).ConfigureAwait(false)
                : _signedAssertionWithInfoDelegate(new AssertionRequestOptions {
                    CancellationToken = cancellationToken,
                    ClientID = requestParameters.AppConfig.ClientId,
                    TokenEndpoint = tokenEndpoint
                }).ConfigureAwait(false));

            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);
            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, signedAssertion);
        }

    }
}

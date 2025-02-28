﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
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
            _signedAssertionWithInfoDelegate = signedAssertionDelegate ?? throw new ArgumentNullException(nameof(signedAssertionDelegate),
                    "Signed assertion delegate cannot be null.");
        }

        public async Task AddConfidentialClientParametersAsync(
            OAuth2Client oAuth2Client,
            AuthenticationRequestParameters requestParameters,
            ICryptographyManager cryptographyManager,
            string tokenEndpoint,
            CancellationToken cancellationToken)
        {
            string assertionType = requestParameters.AppConfig.ClientId == Constants.FmiUrnClientId ?
                                   OAuth2AssertionType.FmiBearer :
                                   OAuth2AssertionType.JwtBearer;
            string signedAssertion;
            if (_signedAssertionDelegate != null)
            {
                // If no "AssertionRequestOptions" delegate is supplied
                signedAssertion = await _signedAssertionDelegate(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Build the AssertionRequestOptions and conditionally set ClientCapabilities
                var assertionOptions = new AssertionRequestOptions
                {
                    CancellationToken = cancellationToken,
                    ClientID = requestParameters.AppConfig.ClientId,
                    TokenEndpoint = tokenEndpoint
                };

                // Set client capabilities 
                var configuredCapabilities = requestParameters
                    .RequestContext
                    .ServiceBundle
                    .Config
                    .ClientCapabilities;

                assertionOptions.ClientCapabilities = configuredCapabilities;

                //Set claims
                assertionOptions.Claims = requestParameters.Claims;

                // Delegate that uses AssertionRequestOptions
                signedAssertion = await _signedAssertionWithInfoDelegate(assertionOptions).ConfigureAwait(false);

            }

            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, assertionType);
            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, signedAssertion);
        }
    }
}

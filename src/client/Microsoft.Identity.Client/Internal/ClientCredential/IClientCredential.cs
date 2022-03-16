// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal interface IClientCredential
    {
        Task AddConfidentialClientParametersAsync(
              OAuth2Client oAuth2Client,
              IMsalLogger logger,
              ICryptographyManager cryptographyManager,
              string clientId,
              string tokenEndpoint,
              bool sendX5C,
              CancellationToken cancellationToken);
    }
}

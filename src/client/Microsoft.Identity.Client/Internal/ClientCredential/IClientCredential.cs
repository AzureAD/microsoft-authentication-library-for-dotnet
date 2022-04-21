// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal interface IClientCredential
    {
        Task AddConfidentialClientParametersAsync(
              OAuth2Client oAuth2Client,
              ILoggerAdapter logger,
              ICryptographyManager cryptographyManager,
              string clientId,
              string tokenEndpoint,
              bool sendX5C,
              CancellationToken cancellationToken);
    }
}

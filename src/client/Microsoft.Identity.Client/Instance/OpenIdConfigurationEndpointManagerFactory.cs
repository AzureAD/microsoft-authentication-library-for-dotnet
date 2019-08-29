// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance
{
    internal static class OpenIdConfigurationEndpointManagerFactory
    {
        public static IOpenIdConfigurationEndpointManager Create(AuthorityInfo authorityInfo, IServiceBundle serviceBundle)
        {
            switch (authorityInfo.AuthorityType)
            {
            case AuthorityType.Adfs:
                return new AdfsOpenIdConfigurationEndpointManager(serviceBundle);
            case AuthorityType.Aad:
                return new AadOpenIdConfigurationEndpointManager(serviceBundle);
            case AuthorityType.B2C:
                return new B2COpenIdConfigurationEndpointManager();
            default:
                throw new InvalidOperationException("Invalid AuthorityType");
            }
        }
    }
}

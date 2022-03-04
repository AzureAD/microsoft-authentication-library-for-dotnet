// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Specifies the source of the access and Id tokens in the authentication result.
    /// </summary>
    public enum TokenSource
    {
        /// <summary>
        /// The source of the access and Id token is Identity Provider - Azure Active Directory (AAD), ADFS or AAD B2C.
        /// </summary>
        IdentityProvider,
        /// <summary>
        /// The source of access and Id token is MSAL's cache.
        /// </summary>
        Cache,
        /// <summary>
        /// The source of the access and Id token is a broker application - Authenticator or Company Portal. Brokers are supported only on Android and iOS.
        /// </summary>
        Broker
    }
}

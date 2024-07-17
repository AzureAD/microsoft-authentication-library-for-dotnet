// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig;

namespace Microsoft.Identity.Client
{
    /// <summary>
    ///
    /// </summary>
    public partial interface IFirstPartyExtension
    {
        /// <summary>
        /// Acquires tokens with contraints
        /// </summary>
        /// <param name="scopes">Scope to request from the token endpoint.
        /// Setting this to null or empty will request an access token, refresh token and ID token with default scopes</param>
        /// <param name="refreshToken">The refresh token from ADAL 2.x</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        //public T WithConstraint(IEnumerable<Constraint> contraints);
    }
}

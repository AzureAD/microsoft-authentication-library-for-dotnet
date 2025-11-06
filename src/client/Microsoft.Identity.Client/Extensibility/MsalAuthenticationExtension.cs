// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Enables the extension of the MSAL authentication process by providing a custom authentication operation. 
    /// These operations are provided through the implementation of the <see cref="IAuthenticationOperation"/> interface.
    /// </summary>
    public class MsalAuthenticationExtension
    {
        /// <summary>
        /// A delegate which gets invoked just before MSAL makes a token request.
        /// </summary>
        public Func<OnBeforeTokenRequestData, Task> OnBeforeTokenRequestHandler { get; set; }

        /// <summary>
        /// Inject an authentication operation. This is used for POP and composite tokens. 
        /// IAuthencationOperation has a KeyID property that is used to bind tokens to a key. This affects how tokens are cached.
        /// 
        /// Important: Use IAuthenticationOperation2 instead, for asynchronous token formatting.
        /// </summary>
        public IAuthenticationOperation AuthenticationOperation { get; set; }

        /// <summary>
        /// Specifies additional parameters acquired from authentication responses to be cached.
        /// </summary>
        public IEnumerable<string> AdditionalCacheParameters { get; set; }
    }
}

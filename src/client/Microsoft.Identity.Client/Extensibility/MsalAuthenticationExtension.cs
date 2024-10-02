// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// TODO: design for 2 things - Test User and CDT
    /// </summary>
    public class MsalAuthenticationExtension

    {
        /// <summary>
        /// 
        /// </summary>
        public Func<OnBeforeTokenRequestData, Task> OnBeforeTokenRequestHandler { get; set; }

        /// <summary>
        /// Enables the developer to provide a custom authentication extension.
        /// </summary>
        /// TODO: guidance on how this interacts with OnBeforeTokenRequestHandler
        public IAuthenticationOperation AuthenticationExtension { get; set; }

        /// <summary>
        /// Specifies additional parameters acquired from authentication responses to be cached
        /// </summary>
        public IEnumerable<string> AdditionalCacheParameters { get; set; }
    }
}

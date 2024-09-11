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
    public class MsalAddIn
    {
        /// <summary>
        /// 
        /// </summary>
        public Func<OnBeforeTokenRequestData, Task> OnBeforeTokenRequestHandler { get; set; }

        /// <summary>
        /// Changes the 
        /// </summary>
        /// TODO: guidance on how this interacts with OnBeforeTokenRequestHandler
        public IAuthenticationScheme AuthenticationScheme { get; set; }
    }
}

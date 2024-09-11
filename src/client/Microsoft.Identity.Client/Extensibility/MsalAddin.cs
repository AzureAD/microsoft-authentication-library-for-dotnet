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

        /// <summary>
        /// When the token endpoint responds with a token, it may include additional properties in the response. This list instructs MSAL to save the properties in the token cache. 
        /// The properties will be returned as part of the <see cref="AuthenticationResult.AdditionalResponseParameters"/> 
        /// </summary>
        /// <remarks>Currently supports only key value properties </remarks>  // TODO: need to model JObject etc, but probably as string
        public IReadOnlyList<string> AdditionalAccessTokenPropertiesToCache { get; set; }  //TODO: bogavril - implement this
    }
}

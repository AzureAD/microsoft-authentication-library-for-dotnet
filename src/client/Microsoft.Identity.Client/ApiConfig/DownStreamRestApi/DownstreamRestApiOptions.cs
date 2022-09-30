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
    /// Down Stream Rest Api Options
    /// Should ApplicationOptions be used instead?
    /// </summary>
    public class DownstreamRestApiOptions : PublicClientApplicationOptions
    {
        /// <summary>
        /// 
        /// </summary>
        public string Authority { get; }

        /// <summary>
        /// 
        /// </summary>
        public string[] Scopes { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="authority"></param>
        /// <param name="scopes"></param>
        public DownstreamRestApiOptions(string clientId, string authority, string[] scopes)
        {
            ClientId = clientId;
            Authority = authority;
            Scopes = scopes;
        }
    }
}

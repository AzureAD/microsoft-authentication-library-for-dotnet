//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Contains authentication parameters based on unauthorized response from resource server.
    /// </summary>
    public sealed partial class AuthenticationParameters
    {
        /// <summary>
        /// Creates authentication parameters from address of the resource. This method expects the resource server to return unauthorized response
        /// with WWW-Authenticate header containing authentication parameters.
        /// </summary>
        /// <param name="resourceUrl">Address of the resource</param>
        /// <returns>AuthenticationParameters object containing authentication parameters</returns>
        public static async Task<AuthenticationParameters> CreateFromResourceUrlAsync(Uri resourceUrl)
        {
            return await CreateFromResourceUrlCommonAsync(resourceUrl);
        }

        /// <summary>
        /// Creates authentication parameters from the response received from the response received from the resource. This method expects the response to have unauthorized status and
        /// WWW-Authenticate header containing authentication parameters.</summary>
        /// <param name="response">Response received from the resource.</param>
        /// <returns>AuthenticationParameters object containing authentication parameters</returns>
        public static AuthenticationParameters CreateFromUnauthorizedResponse(HttpWebResponse response)
        {
            return CreateFromUnauthorizedResponseCommon(NetworkPlugin.HttpWebRequestFactory.CreateResponse(response));
        }
    }
}
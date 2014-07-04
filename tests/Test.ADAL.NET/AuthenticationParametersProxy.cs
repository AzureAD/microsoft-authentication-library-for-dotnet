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
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{
    internal partial class AuthenticationParametersProxy
    {
        internal static async Task<AuthenticationParametersProxy> CreateFromResourceUrlAsync(Uri resourceUrl)
        {
            AuthenticationParameters parameters = await AuthenticationParameters.CreateFromResourceUrlAsync(resourceUrl);
            return new AuthenticationParametersProxy { Authority = parameters.Authority, Resource = parameters.Resource };
        }

        internal static AuthenticationParametersProxy CreateFromResponseAuthenticateHeader(string authenticateHeader)
        {
            AuthenticationParameters parameters = AuthenticationParameters.CreateFromResponseAuthenticateHeader(authenticateHeader);
            return new AuthenticationParametersProxy { Authority = parameters.Authority, Resource = parameters.Resource };
        }

        internal static AuthenticationParametersProxy CreateFromUnauthorizedResponse(HttpWebResponse response)
        {
            AuthenticationParameters parameters = AuthenticationParameters.CreateFromUnauthorizedResponse(response);
            return new AuthenticationParametersProxy { Authority = parameters.Authority, Resource = parameters.Resource };
        }
    }
}

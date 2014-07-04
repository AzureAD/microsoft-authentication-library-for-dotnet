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
using Test.ADAL.WinRT;

namespace Test.ADAL.Common
{
    internal partial class AuthenticationParametersProxy
    {
        internal static async Task<AuthenticationParametersProxy> CreateFromResourceUrlAsync(Uri resourceUrl)
        {
            var result = await AuthenticationContextProxy.AddCommandAndRunAsync(
                CommandType.CreateFromResourceUrlAsync,
                new CommandArguments { Extra = resourceUrl.AbsoluteUri });

            return new AuthenticationParametersProxy { Authority = result.AuthenticationParametersAuthority, Resource = result.AuthenticationParametersResource };
        }

        internal static AuthenticationParametersProxy CreateFromResponseAuthenticateHeader(string authenticateHeader)
        {
            var task = AuthenticationContextProxy.AddCommandAndRunAsync(
                CommandType.CreateFromResponseAuthenticateHeader,
                new CommandArguments { Extra = authenticateHeader });

            var result = task.Result;

            return new AuthenticationParametersProxy { Authority = result.AuthenticationParametersAuthority, Resource = result.AuthenticationParametersResource };
        }

        internal static AuthenticationParametersProxy CreateFromUnauthorizedResponse(HttpWebResponse response)
        {
            // ADAL WinRT does not support this overload
            throw new NotImplementedException();
        }
    }
}

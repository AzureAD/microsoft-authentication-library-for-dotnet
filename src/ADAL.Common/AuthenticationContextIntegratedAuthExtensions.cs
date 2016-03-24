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
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    public static class AuthenticationContextIntegratedAuthExtensions
	{

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <remarks>This feature is supported only for Azure Active Directory and Active Directory Federation Services (ADFS) on Windows 10.</remarks>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userCredential">The user credential to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public static async Task<AuthenticationResult> AcquireTokenAsync(this AuthenticationContext ctx, string resource, string clientId, UserCredential userCredential)
        {
            return await ctx.AcquireTokenCommonAsync(resource, clientId, userCredential);
        }
    }

}

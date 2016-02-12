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
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    public static class PublicClientApplicationExtensions
    {

        //TODO look into adding user identifier when domain cannot be queried or privacy settings are against you
        /// <summary>
        /// .NET specific method for intergrated auth. To support Xamarin, we would need to move these to platform specific libraries.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public static async Task<AuthenticationResult> AcquireTokenWithIntegratedAuthAsync(this PublicClientApplication app, string[] scope)
        {
            return
                await
                    app.AcquireTokenWithIntegratedAuthInternalAsync(scope).ConfigureAwait(false);
        }

        /// <summary>
        /// .NET specific method for intergrated auth. To support Xamarin, we would need to move these to platform specific libraries.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="authority"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        public static async Task<AuthenticationResult> AcquireTokenWithIntegratedAuthAsync(this PublicClientApplication app, string[] scope, string authority, string policy)
        {

            return
                await
                    app.AcquireTokenWithIntegratedAuthInternalAsync(scope, authority, policy).ConfigureAwait(false);
        }
    }
}
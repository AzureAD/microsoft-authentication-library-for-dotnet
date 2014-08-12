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
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// The main class representing the authority issuing tokens for resources.
    /// </summary>
    public sealed partial class AuthenticationContext
    {
        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public IAsyncOperation<AuthenticationResult> AcquireTokenSilentAsync(string resource, string clientId)
        {
            return RunTaskAsAsyncOperation(this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientId), UserIdentifier.AnyUser));
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public IAsyncOperation<AuthenticationResult> AcquireTokenSilentAsync(string resource, string clientId, UserIdentifier userId)
        {
            return RunTaskAsAsyncOperation(this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientId), userId));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public IAsyncOperation<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId)
        {
            return RunTaskAsAsyncOperation(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientId), null));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public IAsyncOperation<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, string resource)
        {
            return RunTaskAsAsyncOperation(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientId), resource));
        }

        private static IAsyncOperation<AuthenticationResult> RunTaskAsAsyncOperation(Task<AuthenticationResult> task)
        {
            return RunTask(task).AsAsyncOperation();
        }

        private static async Task<AuthenticationResult> RunTask(Task<AuthenticationResult> task)
        {
            AuthenticationResult result;

            try
            {
                result = await task;
            }
            catch (Exception ex)
            {
                result = new AuthenticationResult(ex);
            }

            result.ReplaceNullStringPropertiesWithEmptyString();

            return result;
        }
    }
}
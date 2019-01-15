//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// 
    /// </summary>
    public interface IByRefreshToken
    {
        /// <summary>
        /// Acquires an access token from an existing refresh token and stores it and the refresh token into 
        /// the application user token cache, where it will be available for further AcquireTokenSilentAsync calls.
        /// This method can be used in migration to MSAL from ADAL v2 and in various integration 
        /// scenarios where you have a RefreshToken available. 
        /// (see https://aka.ms/msal-net-migration-adal2-msal2)
        /// </summary>
        /// <param name="scopes">Scope to request from the token endpoint.
        /// Setting this to null or empty will request an access token, refresh token and ID token with default scopes</param>
        /// <param name="refreshToken">The refresh token from ADAL 2.x</param>
        Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(IEnumerable<string> scopes, string refreshToken);
    }
}
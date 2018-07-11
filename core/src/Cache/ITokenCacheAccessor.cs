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

using System.Collections.Generic;

namespace Microsoft.Identity.Core.Cache
{
    internal interface ITokenCacheAccessor
    {
        void SaveAccessToken(MsalAccessTokenCacheItem item);

        void SaveRefreshToken(MsalRefreshTokenCacheItem item);

        void SaveIdToken(MsalIdTokenCacheItem item);

        void SaveAccount(MsalAccountCacheItem item);

        string GetAccessToken(MsalAccessTokenCacheKey accessTokenKey);

        string GetRefreshToken(MsalRefreshTokenCacheKey refreshTokenKey);

        string GetIdToken(MsalIdTokenCacheKey idTokenKey);

        string GetAccount(MsalAccountCacheKey accountKey);

        void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey);

        void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey);

        void DeleteIdToken(MsalIdTokenCacheKey cacheKey);

        void DeleteAccount(MsalAccountCacheKey cacheKey);

        ICollection<string> GetAllAccessTokensAsString();

        ICollection<string> GetAllRefreshTokensAsString();

        ICollection<string> GetAllIdTokensAsString();

        ICollection<string> GetAllAccountsAsString();

        /*
        ICollection<string> GetAllAccessTokenKeys();

        ICollection<string> GetAllRefreshTokenKeys();

        ICollection<string> GetAllIdTokenKeys();

        ICollection<string> GetAllAccountKeys();
        */
        void SetSecurityGroup(string securityGroup);

        void Clear();
    }
}

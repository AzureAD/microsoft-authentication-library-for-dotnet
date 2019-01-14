// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    internal class AdalLegacyCacheManager : IAdalLegacyCacheManager
    {
        public AdalLegacyCacheManager(ILegacyCachePersistence legacyCachePersistence)
        {
            LegacyCachePersistence = legacyCachePersistence;
        }

        public ILegacyCachePersistence LegacyCachePersistence { get; }

        /// <inheritdoc />
        public void WriteAdalRefreshToken()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Credential GetAdalRefreshToken()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<Microsoft.Identity.Client.CacheV2.Schema.Account> GetAllAdalUsers()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveAdalUser()
        {
            throw new NotImplementedException();
        }
        void SetIosKeychainSecurityGroup(string securityGroup);

        // These methods are only for test...
        // TODO(migration): separate out into a separate interface?
        ICollection<MsalAccessTokenCacheItem> GetAllAccessTokensForClient(RequestContext requestContext);
        ICollection<MsalRefreshTokenCacheItem> GetAllRefreshTokensForClient(RequestContext requestContext);
        ICollection<MsalIdTokenCacheItem> GetAllIdTokensForClient(RequestContext requestContext);
        ICollection<MsalAccountCacheItem> GetAllAccounts(RequestContext requestContext);
        ILegacyCachePersistence LegacyPersistence { get; }
        ITokenCacheAccessor Accessor { get; }
        void RemoveMsalAccount(IAccount account, RequestContext requestContext);
    }
}
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
using System.Collections.Generic;
using System.Linq;

using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{    
    internal partial class AuthenticationContextProxy
    {
        private const string FixedCorrelationId = "2ddbba59-1a04-43fb-b363-7fb0ae785030";
        private readonly AuthenticationContext context;

        internal void VerifySingleItemInCache(AuthenticationResultProxy result, StsType stsType)
        {
            List<TokenCacheItem> items = this.context.TokenCache.ReadItems().ToList();
            Verify.AreEqual(1, items.Count);
            Verify.AreEqual(result.AccessToken, items[0].AccessToken);
            Verify.AreEqual(result.RefreshToken, items[0].RefreshToken);
            Verify.AreEqual(result.IdToken, items[0].IdToken);
            Verify.IsTrue(stsType == StsType.ADFS || items[0].IdToken != null);
        }
    }
}

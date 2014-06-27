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

using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;

namespace Test.ADAL.Common
{    
    internal partial class AuthenticationContextProxy
    {
        private const string FixedCorrelationId = "2ddbba59-1a04-43fb-b363-7fb0ae785030";
        private AuthenticationContext context;

        public async static Task<AuthenticationContextProxy> CreateProxyAsync(string authority)
        {
            AuthenticationContextProxy proxy = new AuthenticationContextProxy();
            proxy.context = AuthenticationContext.CreateAsync(authority).GetResults();
            proxy.context.CorrelationId = new Guid(FixedCorrelationId);
            return proxy;
        }

        public async static Task<AuthenticationContextProxy> CreateProxyAsync(string authority, bool validateAuthority)
        {
            AuthenticationContextProxy proxy = new AuthenticationContextProxy();
            proxy.context = AuthenticationContext.CreateAsync(authority, validateAuthority).GetResults();
            proxy.context.CorrelationId = new Guid(FixedCorrelationId);
            return proxy;
        }
    }
}

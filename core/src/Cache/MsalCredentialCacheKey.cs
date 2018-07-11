//----------------------------------------------------------------------
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
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Instance;

namespace Microsoft.Identity.Core.Cache
{
    abstract class MsalCredentialCacheKey : MsalCacheKeyBase
    {
        public const string ScopesDelimiter = " ";

        internal MsalCredentialCacheKey(string environment, string tenantId, string userIdentifier,
            CredentialType credentialType, string clientId, string scopes)
            : base(environment, userIdentifier)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            this.CredentialType = credentialType;
            this.ClientId = clientId;
            this.Scopes = scopes;
            this.TenantId = tenantId;
        }

        internal CredentialType CredentialType { get; set; }

        internal string ClientId { get; set; }

        internal string Scopes { get; set; }

        internal string TenantId { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append((HomeAccountId ?? "") + CacheKeyDelimiter);
            stringBuilder.Append(this.Environment + CacheKeyDelimiter);
            stringBuilder.Append(CredentialType + CacheKeyDelimiter);
            stringBuilder.Append(ClientId + CacheKeyDelimiter);
            stringBuilder.Append((TenantId ?? "") + CacheKeyDelimiter);
            stringBuilder.Append(Scopes);

            return stringBuilder.ToString();
        }
    }
}

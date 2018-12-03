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

using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Cache
{
    [DataContract]
    internal class MsalIdTokenCacheItem : MsalCredentialCacheItemBase
    {
        internal MsalIdTokenCacheItem()
        {
            CredentialType = MsalCacheCommon.IdToken;
        }

        internal MsalIdTokenCacheItem(
            string environment,
            string clientId,
            MsalTokenResponse response,
            string tenantId)
            : this(
                environment,
                clientId,
                response.IdToken,
                response.ClientInfo,
                tenantId)
        {
        }

        internal MsalIdTokenCacheItem(
            string environment,
            string clientId,
            string secret,
            string rawClientInfo,
            string tenantId)
            : this()
        {
            Environment = environment;
            TenantId = tenantId;
            ClientId = clientId;
            Secret = secret;
            RawClientInfo = rawClientInfo;

            InitUserIdentifier();
        }

        [DataMember(Name = "realm")]
        internal string TenantId { get; set; }

        internal string Authority =>
            string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/", Environment, TenantId ?? "common");

        internal IdToken IdToken => IdToken.Parse(Secret);

        internal MsalIdTokenCacheKey GetKey()
        {
            return new MsalIdTokenCacheKey(Environment, TenantId, HomeAccountId, ClientId);
        }
    }
}
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

using Microsoft.Identity.Client.CacheV2.Impl.Utils;
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal class MsalCredentialCacheItemBase : MsalCacheItemBase
    {
        internal string CredentialType { get; set; }
        public string ClientId { get; set; }
        public string Secret { get; set; }

        internal override void PopulateFieldsFromJObject(JObject j)
        {
            CredentialType = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.CredentialType);
            ClientId = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.ClientId);
            Secret = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Secret);

            // Important: this MUST be last, since it will extract the AdditionalFieldsJson
            // after all other fields are read.
            base.PopulateFieldsFromJObject(j);
        }

        internal override JObject ToJObject()
        {
            var json = base.ToJObject();
            json[StorageJsonKeys.ClientId] = ClientId;
            json[StorageJsonKeys.Secret] = Secret;
            json[StorageJsonKeys.CredentialType] = CredentialType;

            return json;
        }
    }
}

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
using System.Text;

namespace Microsoft.Identity.Client.Cache.Keys
{
    internal partial class MsalCacheKeys
    {
        public const string CacheKeyDelimiter = "-";


        public static string GetCredentialKey(string homeAccountId, string environment, string keyDescriptor, string clientId, string tenantId, string scopes)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(homeAccountId ?? "");
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(environment); // guaranteed not null
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(keyDescriptor); // a constant
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(clientId);   // guaranteed not null
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(tenantId ?? "");
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(scopes ?? "");

            return stringBuilder.ToString().ToLowerInvariant();
        }

        public static string GetiOSAccountKey(string homeAccountId, string environment)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(homeAccountId ?? "");
            stringBuilder.Append(MsalCacheKeys.CacheKeyDelimiter);

            stringBuilder.Append(environment);

            return stringBuilder.ToString().ToLowerInvariant();
        }


        public static string GetiOSServiceKey(string keyDescriptor, string clientId, string tenantId, string scopes)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(keyDescriptor);
            stringBuilder.Append(MsalCacheKeys.CacheKeyDelimiter);

            stringBuilder.Append(clientId);
            stringBuilder.Append(MsalCacheKeys.CacheKeyDelimiter);

            stringBuilder.Append(tenantId ?? "");
            stringBuilder.Append(MsalCacheKeys.CacheKeyDelimiter);

            stringBuilder.Append(scopes ?? "");

            return stringBuilder.ToString().ToLowerInvariant();
        }

        public static string GetiOSGenericKey(string keyDescriptor, string clientId, string tenantId)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(keyDescriptor);
            stringBuilder.Append(MsalCacheKeys.CacheKeyDelimiter);

            stringBuilder.Append(clientId);
            stringBuilder.Append(MsalCacheKeys.CacheKeyDelimiter);

            stringBuilder.Append(tenantId ?? "");

            return stringBuilder.ToString().ToLowerInvariant();
        }

#region iOS

        internal static readonly Dictionary<string, int> iOSAuthorityTypeToAttrType = new Dictionary<string, int>()
        {
            {CacheAuthorityType.AAD.ToString(), 1001},
            {CacheAuthorityType.MSA.ToString(), 1002},
            {CacheAuthorityType.MSSTS.ToString(), 1003},
            {CacheAuthorityType.OTHER.ToString(), 1004},
        };

        #endregion
    }
}

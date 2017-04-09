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

using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.Identity.Client.Internal.Cache
{
    /// <summary>
    /// Token cache item
    /// </summary>
    [DataContract]
    internal abstract class BaseTokenCacheItem
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public BaseTokenCacheItem(string clientId)
        {
            ClientId = clientId;
        }

        public BaseTokenCacheItem()
        {
        }

        [DataMember(Name = "client_info")]
        public string RawClientInfo { get; set; }

        [DataMember(Name = "client_id")]
        public string ClientId { get; set; }

        public ClientInfo ClientInfo { get; set; }

        public User User { get; set; }

        internal string GetUserIdentifier()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Base64UrlHelpers.Encode(ClientInfo?.UniqueIdentifier),
                Base64UrlHelpers.Encode(ClientInfo?.UniqueTenantIdentifier));
        }
    }
}
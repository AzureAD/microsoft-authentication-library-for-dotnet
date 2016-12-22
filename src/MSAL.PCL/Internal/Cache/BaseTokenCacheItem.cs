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

using System.Runtime.Serialization;
using Microsoft.Identity.Client.Internal.OAuth2;

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
        internal BaseTokenCacheItem(string authority, string clientId, string policy, TokenResponse response)
        {

            if (response.IdToken!=null)
            {
                RawIdToken = response.IdToken;
                IdToken idToken = IdToken.Parse(response.IdToken);
                TenantId = idToken.TenantId;
                User = new User(idToken);
            }

            this.UserAssertionHash = response.UserAssertionHash;
            this.Authority = authority;
            this.ClientId = clientId;
            this.Policy = policy;
        }

        internal BaseTokenCacheItem()
        {
        }

        /// <summary>
        /// Gets the Authority.
        /// </summary>
        [DataMember(Name = "authority")]
        public string Authority { get; set; }

        /// <summary>
        /// Gets the ClientId.
        /// </summary>
        [DataMember(Name = "client_id")]
        public string ClientId { get; set; }

        /// <summary>
        /// Gets the Policy.
        /// </summary>
        [DataMember(Name = "policy")]
        public string Policy { get; set; }

        /// <summary>
        /// Gets the TenantId.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets the user's unique Id.
        /// </summary>
        public string UniqueId { get { return User?.UniqueId; } }

        /// <summary>
        /// Gets the user's displayable Id.
        /// </summary>
        public string DisplayableId { get { return User?.DisplayableId; } }

        public string HomeObjectId { get { return User?.HomeObjectId; } }
        
        [DataMember(Name = "id_token")]
        public string RawIdToken { get; set; }

        /// <summary>
        /// Gets the entire Profile Info if returned by the service or null if no Id Token is returned.
        /// </summary>
        public User User { get; set; }

        [DataMember(Name = "user_assertion_hash")]
        public string UserAssertionHash { get; set; }

        public abstract TokenCacheKey GetTokenCacheKey();
        
        // This method is called after the object 
        // is completely deserialized.
        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            if (RawIdToken != null)
            {
                IdToken idToken = IdToken.Parse(RawIdToken);
                TenantId = idToken.TenantId;
                User = new User(idToken);
            }
        }
    }
}
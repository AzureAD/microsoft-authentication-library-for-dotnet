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

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal.Cache
{
    /// <summary>
    /// Token cache item
    /// </summary>
    internal abstract class BaseTokenCacheItem
    {
        private readonly User _user;

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
                _user = new User(idToken);
            }

            this.Authority = authority;
            this.ClientId = clientId;
            this.Policy = policy;
            Scope = response.Scope.AsSet();
            
        }

        /// <summary>
        /// Gets the Authority.
        /// </summary>
        public string Authority { get; }

        /// <summary>
        /// Gets the ClientId.
        /// </summary>
        public string ClientId { get; internal set; }
        
        /// <summary>
        /// Gets the IdentityProviderName.
        /// </summary>
        public string IdentityProvider { get; internal set; }

        /// <summary>
        /// Gets the Scope.
        /// </summary>
        public HashSet<string> Scope { get; internal set; }

        /// <summary>
        /// Gets the Policy.
        /// </summary>
        public string Policy { get; internal set; }

        /// <summary>
        /// Gets the TenantId.
        /// </summary>
        public string TenantId { get; internal set; }

        /// <summary>
        /// Gets the user's unique Id.
        /// </summary>
        public string UniqueId { get { return _user?.UniqueId; } }

        /// <summary>
        /// Gets the user's displayable Id.
        /// </summary>
        public string DisplayableId { get { return _user?.DisplayableId; } }

        public string HomeObjectId { get { return _user?.HomeObjectId; } }


        public string RawIdToken { get; }

        /// <summary>
        /// Gets the entire Profile Info if returned by the service or null if no Id Token is returned.
        /// </summary>
        public User User { get; internal set; }
    }
}
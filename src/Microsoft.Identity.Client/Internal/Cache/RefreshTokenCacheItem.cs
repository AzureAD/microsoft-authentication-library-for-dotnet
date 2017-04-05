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
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal.Cache
{
    [DataContract]
    internal class RefreshTokenCacheItem : BaseTokenCacheItem
    {

        public RefreshTokenCacheItem()
        {
        }

        public RefreshTokenCacheItem(string environment, string clientId, TokenResponse response) : base(clientId)
        {
            RefreshToken = response.RefreshToken;
            Environment = environment;
            PopulateIdentifiers(response);
        }

        [DataMember(Name = "environment")]
        public string Environment { get; set; }

        [DataMember(Name = "displayable_id")]
        public string DisplayableId { get; internal set; }

        [DataMember(Name = "name")]
        public string Name { get; internal set; }

        [DataMember(Name = "identity_provider")]
        public string IdentityProvider { get; internal set; }

        [DataMember (Name = "refresh_token")]
        public string RefreshToken { get; set; }

        public RefreshTokenCacheKey GetRefreshTokenItemKey()
        {
            return new RefreshTokenCacheKey(Environment, ClientId, GetUserIdentifier());
        }

        public void PopulateIdentifiers(TokenResponse response)
        {
            IdToken idToken = IdToken.Parse(response.IdToken);
            RawClientInfo = response.ClientInfo;
            ClientInfo = ClientInfo.Parse(RawClientInfo);
            
            DisplayableId = idToken.PreferredUsername;
            Name = idToken.Name;
            IdentityProvider = idToken.Issuer;

            User = new User(GetUserIdentifier(), DisplayableId, Name, IdentityProvider);
        }

        // This method is called after the object 
        // is completely deserialized.
        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            ClientInfo = ClientInfo.Parse(RawClientInfo);
            User = new User(GetUserIdentifier(), DisplayableId, Name, IdentityProvider);
        }
    }
}

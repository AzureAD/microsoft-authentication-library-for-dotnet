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

            User = User.Create(DisplayableId, Name, IdentityProvider,
                GetUserIdentifier());
        }

        [DataMember(Name = "environment")]
        public string Environment { get; set; }

        [DataMember(Name = "uid")]
        public string Uid { get; set; }

        [DataMember(Name = "utid")]
        public string Utid { get; set; }

        [DataMember(Name = "displayable_id")]
        public string DisplayableId { get; internal set; }

        [DataMember(Name = "name")]
        public string Name { get; internal set; }

        [DataMember(Name = "idp")]
        public string IdentityProvider { get; internal set; }

        [DataMember (Name = "refresh_token")]
        public string RefreshToken { get; set; }

        public RefreshTokenCacheKey GetRefreshTokenItemKey()
        {
            return new RefreshTokenCacheKey(Environment, ClientId, GetUserIdentifier());
        }

        public void PopulateIdentifiers(TokenResponse response)
        {
            ClientInfo info = ClientInfo.Parse(response.ClientInfo);
            IdToken idToken = IdToken.Parse(response.IdToken);
            if (info != null)
            {
                Uid = info.UniqueIdentifier;
                Utid = info.UniqueTenantIdentifier;
            }
            else
            {
                Uid = idToken.GetUniqueId();
                Utid = idToken.TenantId;
            }

            DisplayableId = idToken.PreferredUsername;
            Name = idToken.Name;
            IdentityProvider = idToken.Issuer;
        }

        public sealed override string GetUserIdentifier()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", MsalHelpers.EncodeToBase64Url(Uid),
                MsalHelpers.EncodeToBase64Url(Utid));
        }

        // This method is called after the object 
        // is completely deserialized.
        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            User = User.Create(DisplayableId, Name, IdentityProvider,
                GetUserIdentifier());
        }
    }
}

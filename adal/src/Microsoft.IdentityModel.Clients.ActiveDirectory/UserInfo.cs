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
using System.Runtime.Serialization;
using Microsoft.Identity.Core.Cache;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Contains information of a single user. This information is used for token cache lookup. Also if created with userId, userId is sent to the service when login_hint is accepted.
    /// </summary>
    [DataContract]
    public sealed class UserInfo
    {
        /// <summary>
        /// Create user information for token cache lookup
        /// </summary>
        public UserInfo()
        {
        }

        /// <summary>
        /// Create user information for token cache lookup
        /// </summary>
        internal UserInfo(AdalUserInfo adalUserInfo) : this(adalUserInfo?.UniqueId, adalUserInfo?.DisplayableId,
            adalUserInfo?.GivenName, adalUserInfo?.FamilyName, adalUserInfo?.IdentityProvider,
            adalUserInfo?.PasswordChangeUrl, adalUserInfo?.PasswordExpiresOn)
        {
        }

        /// <summary>
        /// Create user information copied from another UserInfo object
        /// </summary>
        public UserInfo(UserInfo other) : this(other?.UniqueId, other?.DisplayableId, other?.GivenName,
            other?.FamilyName, other?.IdentityProvider, other?.PasswordChangeUrl, other?.PasswordExpiresOn)
        {
        }

        private UserInfo(string uniqueId, string displayableId, string givenName, string familyName,
            string identityProvider, Uri passwordChangeUrl, DateTimeOffset? passwordExpiresOn)
        {
            this.UniqueId = uniqueId;
            this.DisplayableId = displayableId;
            this.GivenName = givenName;
            this.FamilyName = familyName;
            this.IdentityProvider = identityProvider;
            this.PasswordChangeUrl = passwordChangeUrl;
            this.PasswordExpiresOn = passwordExpiresOn;
        }

        /// <summary>
        /// Gets identifier of the user authenticated during token acquisition. 
        /// </summary>
        [DataMember]
        public string UniqueId { get; internal set; }

        /// <summary>
        /// Gets a displayable value in UserPrincipalName (UPN) format. The value can be null.
        /// </summary>
        [DataMember]
        public string DisplayableId { get; internal set; }

        /// <summary>
        /// Gets given name of the user if provided by the service. If not, the value is null. 
        /// </summary>
        [DataMember]
        public string GivenName { get; internal set; }

        /// <summary>
        /// Gets family name of the user if provided by the service. If not, the value is null. 
        /// </summary>
        [DataMember]
        public string FamilyName { get; internal set; }

        /// <summary>
        /// Gets the time when the password expires. Default value is 0.
        /// </summary>
        [DataMember]
        public DateTimeOffset? PasswordExpiresOn { get; internal set; }

        /// <summary>
        /// Gets the url where the user can change the expiring password. The value can be null.
        /// </summary>
        [DataMember]
        public Uri PasswordChangeUrl { get; internal set; }

        /// <summary>
        /// Gets identity provider if returned by the service. If not, the value is null. 
        /// </summary>
        [DataMember]
        public string IdentityProvider { get; internal set; }
    }
}
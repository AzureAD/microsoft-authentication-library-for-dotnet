//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Contains information of a single user. This information is used for token cache lookup. Also if created with userId, userId is sent to the service when login_hint is accepted.
    /// </summary>
    [DataContract]
    public sealed class UserInfo
    {
        internal UserInfo()
        {            
        }

        internal UserInfo(string userId)
        {
            this.UserId = userId;
        }

        internal UserInfo(UserInfo other)
        {
            this.UserId = other.UserId;
            this.IsUserIdDisplayable = other.IsUserIdDisplayable;
            this.GivenName = other.GivenName;
            this.FamilyName = other.FamilyName;
            this.IdentityProvider = other.IdentityProvider;
        }

        /// <summary>
        /// Gets identifier of the user authenticated during token acquisition. 
        /// </summary>
        [DataMember]
        public string UserId { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether UserId is displayable or not.
        /// </summary>
        [DataMember]
        public bool IsUserIdDisplayable { get; internal set; }

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
        /// Gets identity provider if returned by the service. If not, the value is null. 
        /// </summary>
        [DataMember]
        public string IdentityProvider { get; internal set; }

        internal bool ForcePrompt { get; private set; }

    }
}

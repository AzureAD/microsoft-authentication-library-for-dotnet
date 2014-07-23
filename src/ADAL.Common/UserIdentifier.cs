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

using System;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Indicates the type of UserIdentifier
    /// </summary>
    public enum UserIdentifierType
    {
        /// <summary>
        /// UniqueId
        /// </summary>
        UniqueId,
        /// <summary>
        /// OptionalDisplayableId
        /// </summary>
        OptionalDisplayableId,
        /// <summary>
        /// RequiredDisplayableId
        /// </summary>
        RequiredDisplayableId,
    }

    /// <summary>
    /// Contains identifier for a user.
    /// </summary>
    public sealed class UserIdentifier
    {
        private const string AnyUserId = "AnyUser";
        private static readonly UserIdentifier AnyUserSingleton = new UserIdentifier(AnyUserId, UserIdentifierType.UniqueId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        public UserIdentifier(string id, UserIdentifierType type)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            this.Id = id;
            this.Type = type;
        }

        /// <summary>
        /// Gets type of the <see cref="UserIdentifier"/>.
        /// </summary>
        public UserIdentifierType Type { get; private set; }
        
        /// <summary>
        /// Gets Id of the <see cref="UserIdentifier"/>.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets an static instance of <see cref="UserIdentifier"/> to represent any user.
        /// </summary>
        public static UserIdentifier AnyUser { 
            get
            {
                return AnyUserSingleton;
            }
        }

        internal bool IsAnyUser 
        {
            get
            {
                return (this.Type == AnyUser.Type && this.Id == AnyUser.Id);
            }            
        }

        internal string UniqueId
        {
            get
            {
                return (!this.IsAnyUser && this.Type == UserIdentifierType.UniqueId) ? this.Id : null;
            }
        }

        internal string DisplayableId
        {
            get
            {
                return (!this.IsAnyUser && (this.Type == UserIdentifierType.OptionalDisplayableId || this.Type == UserIdentifierType.RequiredDisplayableId)) ? this.Id : null;
            }
        }
    }

}

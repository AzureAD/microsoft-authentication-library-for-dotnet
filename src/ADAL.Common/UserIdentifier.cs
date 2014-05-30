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
        /// 
        /// </summary>
        UniqueId,
        /// <summary>
        /// 
        /// </summary>
        OptionalDisplayableId,
        /// <summary>
        /// 
        /// </summary>
        RequiredDisplayableId,
    }

    /// <summary>
    /// Contains identifier for a user.
    /// </summary>
    public sealed class UserIdentifier
    {
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
        /// 
        /// </summary>
        public UserIdentifierType Type { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public string Id { get; private set; }
    }

}

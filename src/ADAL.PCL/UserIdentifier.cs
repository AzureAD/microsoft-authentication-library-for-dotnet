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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Indicates the type of <see cref=" UserIdentifier"/>
    /// </summary>
    public enum UserIdentifierType
    {
        /// <summary>
        /// When a <see cref=" UserIdentifier"/> of this type is passed in a token acquisition operation,
        /// the operation is guaranteed to return a token issued for the user with corresponding <see cref=" UserIdentifier.UniqueId"/> or fail.
        /// </summary>
        UniqueId,

        /// <summary>
        /// When a <see cref=" UserIdentifier"/> of this type is passed in a token acquisition operation,
        /// the operation restricts cache matches to the value provided and injects it as a hint in the authentication experience. However the end user could overwrite that value, resulting in a token issued to a different account than the one specified in the <see cref=" UserIdentifier"/> in input.
        /// </summary>
        OptionalDisplayableId,
        
        /// <summary>
        /// When a <see cref=" UserIdentifier"/> of this type is passed in a token acquisition operation,
        /// the operation is guaranteed to return a token issued for the user with corresponding <see cref=" UserIdentifier.DisplayableId"/> (UPN or email) or fail
        /// </summary>
        RequiredDisplayableId
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

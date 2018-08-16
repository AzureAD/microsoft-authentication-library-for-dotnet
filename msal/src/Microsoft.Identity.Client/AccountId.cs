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


using Microsoft.Identity.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// An identifier for an account in a specific tenant
    /// </summary>
    public class AccountId
    {
        /// <summary>
        /// Unique identifier for the account
        /// </summary>
        /// <remarks>
        /// For Azure AD, the identifier is the concatenation of <see cref="ObjectId"/>and <see cref="TenantId"/> separated by a dot. 
        /// Contrary to what was happening in ADAL.NET, these two segments are no longer base64 encoded.
        /// </remarks>
        public string Identifier { get; }

        /// <summary>
        /// For Azure AD, a string representation for a Guid which is the Object ID of the user owning the account in the tenant
        /// </summary>
        public string ObjectId { get;  }

        /// <summary>
        /// For Azure AD, a string representation for a Guid, which is the ID of the tenant where the account resides.
        /// </summary>
        public string TenantId { get;  }

        /// <summary>
        /// Constructor of an AccountId
        /// </summary>
        /// <param name="identifier">Unique identifier for the account.</param>
        /// <param name="objectId">A string representation for a GUID which is the ID of the user owning the account in the tenant</param>
        /// <param name="tenantId">A string representation for a GUID, which is the ID of the tenant where the account resides</param>
        public AccountId(string identifier, string objectId, string tenantId)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            this.Identifier = identifier;
            this.ObjectId = objectId;
            this.TenantId = tenantId;

            ValidateId();
        }

      

        #region Adapter to / from ClientInfo
        internal static AccountId FromClientInfo(ClientInfo clientInfo)
        {
            if (clientInfo == null)
            {
                throw new ArgumentNullException(nameof(clientInfo));
            }

            return new AccountId(
                clientInfo.ToAccountIdentifier(),
                clientInfo.UniqueObjectIdentifier,
                clientInfo.UniqueTenantIdentifier);
        }

        internal ClientInfo ToClientInfo()
        {
            return new ClientInfo()
            {
                UniqueObjectIdentifier = this.ObjectId,
                UniqueTenantIdentifier = this.TenantId
            };
        }

        #endregion

        /// <summary>
        /// Two accounts are equal when their <see cref="Identifier"/> properties match
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null) { return false; }

            var otherMsalAccountId = obj as AccountId;
            if (otherMsalAccountId == null) { return false; }

            return this.Identifier == otherMsalAccountId.Identifier;
        }

        /// <summary>
        /// GetHashCode implementation to match <see cref="Equals(object)"/>
        /// </summary>
        public override int GetHashCode()
        {
            return this.Identifier.GetHashCode();
        }

        /// <summary>
        /// Textual description of an <see cref="AccountId"/>
        /// </summary>
        public override string ToString()
        {
            return "AccountId: " + this.Identifier;
        }

        [Conditional("DEBUG")]
        private void ValidateId()
        {
            string expectedId = this.ObjectId + "." + this.TenantId;
            if (!String.Equals(expectedId, this.Identifier, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Internal Error (debug only) - " +
                    "Expecting Identifer = ObjectId.TenantId but have " + this.ToString());
            }
        }
    }
}

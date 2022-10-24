// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// An identifier for an account in a specific tenant. Returned by <see cref="P:IAccount.HomeAccountId"/>
    /// </summary>
    public class AccountId
    {
        /// <summary>
        /// Unique identifier for the account
        /// </summary>
        /// <remarks>
        /// For the Microsoft identity platform (formerly named Azure AD v2.0), the identifier is the concatenation of
        /// <see cref="ObjectId"/> and <see cref="TenantId"/> separated by a dot.
        /// Contrary to what was happening in ADAL.NET, these two segments are no longer base64 encoded.
        /// Note that there are some legitimate cases (for instance domain takeover), where the same ObjectId may show up in multiple tenants.
        /// </remarks>
        public string Identifier { get; }

        /// <summary>
        /// For Azure AD, a string representation for a GUID which is the Object ID of the user owning the account in the tenant
        /// </summary>
        public string ObjectId { get; }

        /// <summary>
        /// For Azure AD, a string representation for a GUID, which is the ID of the tenant where the account resides.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Constructor of an AccountId
        /// </summary>
        /// <param name="identifier">Unique identifier for the account.</param>
        /// <param name="objectId">A string representation for a GUID which is the ID of the user owning the account in the tenant</param>
        /// <param name="tenantId">A string representation for a GUID, which is the ID of the tenant where the account resides</param>
        public AccountId(string identifier, string objectId, string tenantId)
        {
            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            ObjectId = objectId;
            TenantId = tenantId;

            ValidateId();
        }

        /// <summary>
        /// Constructor of an AccountId meant for ADFS scenarios since ADFS instances lack tenant ids.
        /// </summary>
        /// <param name="adfsIdentifier">Unique identifier for the account if authority is ADFS</param>
        public AccountId(string adfsIdentifier)
            : this(adfsIdentifier, adfsIdentifier, null)
        { }

        internal static AccountId ParseFromString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            int lastIndexOfDot = str.LastIndexOf('.');

            if (lastIndexOfDot == -1)
            {
                return new AccountId(str); //Account id is from ADFS; no . in the string
            }

            return new AccountId(str, str.Substring(0, lastIndexOfDot), str.Substring(lastIndexOfDot + 1));
        }

        /// <summary>
        /// Two accounts are equal when their <see cref="Identifier"/> properties match
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is AccountId otherMsalAccountId))
            {
                return false;
            }

            return string.Equals(Identifier, otherMsalAccountId.Identifier, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// GetHashCode implementation to match <see cref="Equals(object)"/>
        /// </summary>
        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }

        /// <summary>
        /// Textual description of an <see cref="AccountId"/>
        /// </summary>
        public override string ToString()
        {
            return "AccountId: " + Identifier;
        }

        [Conditional("DEBUG")]
        private void ValidateId()
        {
            string expectedId = TenantId == null ? ObjectId : ObjectId + "." + TenantId;
            if (!string.Equals(expectedId, Identifier, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Internal Error (debug only) - " +
                    "Expecting Identifier = ObjectId.TenantId but have " + ToString());
            }
        }
    }
}

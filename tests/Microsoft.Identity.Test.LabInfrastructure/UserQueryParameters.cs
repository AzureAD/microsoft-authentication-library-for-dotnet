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
using System.Collections.Generic;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class UserQuery
    {
        public FederationProvider? FederationProvider { get; set; }
        public bool? IsMamUser { get; set; }
        public bool? IsMfaUser { get; set; }
        public ISet<string> Licenses { get; set; }
        public bool? IsFederatedUser { get; set; }
        public UserType? UserType { get; set; }
        public bool? IsExternalUser { get; set; }
        public B2CIdentityProvider? B2CIdentityProvider { get; set; }
        public string UserSearch { get; set; }
        public string AppName { get; set; }

        public static UserQuery DefaultUserQuery => new UserQuery
        {
            IsMamUser = false,
            IsMfaUser = false,
            IsFederatedUser = false
        };

        public static UserQuery B2CLocalAccountUserQuery => new UserQuery
        {
            UserType = LabInfrastructure.UserType.B2C,
            B2CIdentityProvider = LabInfrastructure.B2CIdentityProvider.Local
        };

        public static UserQuery B2CFacebookUserQuery => new UserQuery
        {
            UserType = LabInfrastructure.UserType.B2C,
            B2CIdentityProvider = LabInfrastructure.B2CIdentityProvider.Facebook
        };

        public static UserQuery B2CGoogleUserQuery => new UserQuery
        {
            UserType = LabInfrastructure.UserType.B2C,
            B2CIdentityProvider = LabInfrastructure.B2CIdentityProvider.Google
        };

        // generated code, re-generate or update manually if you change the members of this class !
        #region Equals and GetHashCode
        public override bool Equals(object obj)
        {
            return Equals(obj as UserQuery);
        }

        public bool Equals(UserQuery other)
        {
            return other != null &&
                   EqualityComparer<FederationProvider?>.Default.Equals(FederationProvider, other.FederationProvider) &&
                   EqualityComparer<bool?>.Default.Equals(IsMamUser, other.IsMamUser) &&
                   EqualityComparer<bool?>.Default.Equals(IsMfaUser, other.IsMfaUser) &&
                   EqualityComparer<ISet<string>>.Default.Equals(Licenses, other.Licenses) &&
                   EqualityComparer<bool?>.Default.Equals(IsFederatedUser, other.IsFederatedUser) &&
                   EqualityComparer<UserType?>.Default.Equals(UserType, other.UserType) &&
                   EqualityComparer<bool?>.Default.Equals(IsExternalUser, other.IsExternalUser) &&
                   EqualityComparer<B2CIdentityProvider?>.Default.Equals(B2CIdentityProvider, other.B2CIdentityProvider);
        }

        public override int GetHashCode()
        {
            var hashCode = 1863312741;
            hashCode = hashCode * -1521134295 + EqualityComparer<FederationProvider?>.Default.GetHashCode(FederationProvider);
            hashCode = hashCode * -1521134295 + EqualityComparer<bool?>.Default.GetHashCode(IsMamUser);
            hashCode = hashCode * -1521134295 + EqualityComparer<bool?>.Default.GetHashCode(IsMfaUser);
            hashCode = hashCode * -1521134295 + EqualityComparer<ISet<string>>.Default.GetHashCode(Licenses);
            hashCode = hashCode * -1521134295 + EqualityComparer<bool?>.Default.GetHashCode(IsFederatedUser);
            hashCode = hashCode * -1521134295 + EqualityComparer<UserType?>.Default.GetHashCode(UserType);
            hashCode = hashCode * -1521134295 + EqualityComparer<bool?>.Default.GetHashCode(IsExternalUser);
            hashCode = hashCode * -1521134295 + EqualityComparer<B2CIdentityProvider?>.Default.GetHashCode(B2CIdentityProvider);
            return hashCode;
        }
        #endregion
    }
}

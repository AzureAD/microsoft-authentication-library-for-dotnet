// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class UserQuery
    {
        public UserType? UserType { get; set; }
        public MFA? MFA { get; set; }
        public ProtectionPolicy? ProtectionPolicy { get; set; }
        public HomeDomain? HomeDomain { get; set; }
        public HomeUPN? HomeUPN { get; set; }
        public B2CIdentityProvider? B2CIdentityProvider { get; set; }
        public FederationProvider? FederationProvider { get; set; } //Requires userType to be set to federated
        public AzureEnvironment? AzureEnvironment { get; set; }
        public SignInAudience? SignInAudience { get; set; }

        public static UserQuery DefaultUserQuery => new UserQuery()
            {
                UserType = LabInfrastructure.UserType.Cloud,
                AzureEnvironment = LabInfrastructure.AzureEnvironment.azurecloud
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

        public static UserQuery B2CMSAUserQuery => new UserQuery
        {
            UserType = LabInfrastructure.UserType.B2C,
            B2CIdentityProvider = LabInfrastructure.B2CIdentityProvider.MSA
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
                EqualityComparer<UserType?>.Default.Equals(UserType, other.UserType) &&
                EqualityComparer<MFA?>.Default.Equals(MFA, other.MFA) &&
                EqualityComparer<ProtectionPolicy?>.Default.Equals(ProtectionPolicy, other.ProtectionPolicy) &&
                EqualityComparer<HomeDomain?>.Default.Equals(HomeDomain, other.HomeDomain) &&
                EqualityComparer<HomeUPN?>.Default.Equals(HomeUPN, other.HomeUPN) &&
                EqualityComparer<B2CIdentityProvider?>.Default.Equals(B2CIdentityProvider, other.B2CIdentityProvider) &&
                EqualityComparer<FederationProvider?>.Default.Equals(FederationProvider, other.FederationProvider) &&
                EqualityComparer<AzureEnvironment?>.Default.Equals(AzureEnvironment, other.AzureEnvironment) &&
                EqualityComparer<SignInAudience?>.Default.Equals(SignInAudience, other.SignInAudience);
        }

        public override int GetHashCode()
        {
            var hashCode = 1863312741;
            hashCode = hashCode * -1521134295 + EqualityComparer<UserType?>.Default.GetHashCode(UserType);
            hashCode = hashCode * -1521134295 + EqualityComparer<MFA?>.Default.GetHashCode(MFA);
            hashCode = hashCode * -1521134295 + EqualityComparer<ProtectionPolicy?>.Default.GetHashCode(ProtectionPolicy);
            hashCode = hashCode * -1521134295 + EqualityComparer<HomeDomain?>.Default.GetHashCode(HomeDomain);
            hashCode = hashCode * -1521134295 + EqualityComparer<HomeUPN?>.Default.GetHashCode(HomeUPN);
            hashCode = hashCode * -1521134295 + EqualityComparer<B2CIdentityProvider?>.Default.GetHashCode(B2CIdentityProvider);
            hashCode = hashCode * -1521134295 + EqualityComparer<FederationProvider?>.Default.GetHashCode(FederationProvider);
            hashCode = hashCode * -1521134295 + EqualityComparer<AzureEnvironment?>.Default.GetHashCode(AzureEnvironment);
            hashCode = hashCode * -1521134295 + EqualityComparer<SignInAudience?>.Default.GetHashCode(SignInAudience);
            return hashCode;
        }
        #endregion
    }
}

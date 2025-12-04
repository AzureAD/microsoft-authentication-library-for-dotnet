// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public struct UserQuery
    {
        public UserType? UserType { get; set; }
        public B2CIdentityProvider? B2CIdentityProvider { get; set; }
        public FederationProvider? FederationProvider { get; set; } //Requires userType to be set to federated
        public AzureEnvironment? AzureEnvironment { get; set; }
        public SignInAudience? SignInAudience { get; set; }
        public AppPlatform? AppPlatform { get; set; }
        public PublicClient? PublicClient { get; set; }

        /// <summary>
        /// Ask for a specific user from the lab. No other parameters will be considered.
        /// </summary>
        public string Upn { get; set; }

        public static UserQuery PublicAadUserQuery => new UserQuery()
        {
            Upn = "MSAL-User-Default@id4slab1.onmicrosoft.com"            
        };

        public static UserQuery PublicAadUser2Query => new UserQuery()
        {
            Upn = "MSAL-User-Default2@id4slab1.onmicrosoft.com"
        };

        public static UserQuery PublicAadUser3Query => new UserQuery()
        {
            Upn = "MSAL-User-XCG@id4slab1.onmicrosoft.com"
        };

        public static UserQuery MsaUserQuery => new UserQuery
        {
            UserType = LabInfrastructure.UserType.MSA
        };

        public static UserQuery B2CLocalAccountUserQuery => new UserQuery
        {
            UserType = LabInfrastructure.UserType.B2C,
            B2CIdentityProvider = LabInfrastructure.B2CIdentityProvider.Local
        };

        public static UserQuery ArlingtonUserQuery => new UserQuery
        {
            UserType = LabInfrastructure.UserType.Cloud,
            AzureEnvironment = LabInfrastructure.AzureEnvironment.azureusgovernment
        };

        public static UserQuery HybridSpaUserQuery => new UserQuery
        {
            UserType = LabInfrastructure.UserType.Cloud,
            AppPlatform = LabInfrastructure.AppPlatform.spa,
            PublicClient = LabInfrastructure.PublicClient.no,
            SignInAudience = LabInfrastructure.SignInAudience.AzureAdMyOrg

        };
    }
}

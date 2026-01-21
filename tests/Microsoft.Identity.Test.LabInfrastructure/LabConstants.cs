// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Test.LabInfrastructure
{

    public enum Cloud
    {
        Public,
        Adfs,
        Arlington
    }

    /// <summary>
    /// Constants for lab user configuration values used in testing.
    /// </summary>
    public static class LabConstants
    {
        // FederationProvider values
        public const string FederationProviderNone = "None";

        // B2CIdentityProvider values
        public const string B2CIdentityProviderLocal = "Local";
        public const string B2CIdentityProviderFacebook = "Facebook";
        public const string B2CIdentityProviderGoogle = "Google";

        // UserType values
        public const string UserTypeB2C = "B2C";
        public const string UserTypeFederated = "Federated";
        public const string UserTypeMSA = "MSA";

        // AzureEnvironment values
        public const string AzureEnvironmentUsGovernment = "azureusgovernment";
    }
}

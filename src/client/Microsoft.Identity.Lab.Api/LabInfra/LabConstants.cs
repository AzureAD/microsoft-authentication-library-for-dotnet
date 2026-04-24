// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Test.LabInfrastructure
{

    /// <summary>
    /// Specifies the available cloud environments for authentication and resource access.
    /// </summary>
    /// <remarks>Use this enumeration to select the appropriate cloud environment when configuring
    /// authentication or connecting to cloud-based services. The values represent distinct environments, such as public
    /// or government-specific clouds, and may affect endpoint URLs, supported features, and compliance
    /// requirements.</remarks>
    public enum Cloud
    {
        /// <summary>
        /// Public cloud environment, representing the standard Azure environment used by most customers and services.
        /// </summary>
        Public,
        /// <summary>
        /// Adfs
        /// </summary>
        Adfs,
        /// <summary>
        /// Arlington
        /// </summary>
        Arlington
    }

    /// <summary>
    /// Constants for lab user configuration values used in testing.
    /// </summary>
    public static class LabConstants
    {
        /// <summary>
        /// FederationProvider value indicating that no federation provider is configured for the user.
        /// </summary>
        public const string FederationProviderNone = "None";

        // B2CIdentityProvider values
        /// <summary>
        /// b2c identity provider value indicating that the user is a local account (i.e. username and password stored in the B2C directory).
        /// </summary>
        public const string B2CIdentityProviderLocal = "Local";
        /// <summary>
        /// b2c identity provider value indicating that the user is a social account federated through Facebook.
        /// </summary>
        public const string B2CIdentityProviderFacebook = "Facebook";
        /// <summary>
        /// b2c identity provider value indicating that the user is a social account federated through Google.
        /// </summary>
        public const string B2CIdentityProviderGoogle = "Google";

        // UserType values
        /// <summary>
        /// Represents the user type value for Business-to-Consumer (B2C) users.
        /// </summary>
        public const string UserTypeB2C = "B2C";
        /// <summary>
        /// Represents the user type value for federated users.
        /// </summary>
        public const string UserTypeFederated = "Federated";
        /// <summary>
        /// Represents the user type identifier for Microsoft Account (MSA) users.
        /// </summary>
        public const string UserTypeMSA = "MSA";

        // AzureEnvironment values
        /// <summary>
        /// Represents the identifier for the Azure US Government environment.
        /// </summary>
        /// <remarks>Use this constant when specifying the Azure environment for services or
        /// authentication targeting the US Government cloud. The value corresponds to the environment name recognized
        /// by Azure APIs and SDKs.</remarks>
        public const string AzureEnvironmentUsGovernment = "azureusgovernment";
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Test.LabInfrastructure
{
    /// <summary>
    /// Contains names of secrets stored in Azure Key Vault for lab configuration.
    /// These secrets contain JSON-serialized configuration objects for users, apps, and lab environments.
    /// </summary>
    public static class KeyVaultSecrets
    {
        /// <summary>
        /// Represents the configuration key for user public cloud settings.
        /// </summary>
        public const string UserPublicCloud = "User-PublicCloud-Config";
        /// <summary>
        /// Represents the configuration key for federated user settings.
        /// </summary>
        public const string UserFederated = "User-Federated-Config";
        /// <summary>
        /// Represents the configuration key for user public cloud settings (alternative).
        /// </summary>
        public const string UserPublicCloud2 = "MSAL-User-Default2-JSON";
        /// <summary>
        /// Represents the configuration key for user XCG settings.
        /// </summary>
        public const string UserXcg = "MSAL-User-XCG-JSON";
        /// <summary>
        /// Represents the configuration key for B2C user settings.
        /// </summary>
        public const string UserB2C = "MSAL-USER-B2C-JSON";
        /// <summary>
        /// Represents the configuration key for Arlington user settings.
        /// </summary>  
        public const string UserArlington = "MSAL-USER-Arlington-JSON";
        /// <summary>
        /// Represents the configuration key for CIAM user settings.
        /// </summary>
        public const string UserCiam = "MSAL-USER-CIAM-JSON";
        /// <summary>
        /// Represents the configuration key for POP user settings.
        /// </summary>
        public const string UserPop = "MSAL-User-POP-JSON";

        // Names of key vault secrets for application configuration JSONs
        //  - Broad test scenarios
        /// <summary>
        /// application configuration for testing server-to-server (S2S) authentication scenarios, which may involve client credentials flow, certificate-based authentication, or other non-interactive authentication methods. This configuration would typically include details such as the application (client) ID, tenant ID, authority, and any necessary secrets or certificates required for S2S authentication.
        /// </summary>
        public const string AppS2S = "App-S2S-Config";
        /// <summary>
        /// Application configuration for testing scenarios involving the Microsoft Authentication Library (MSAL) in a client application context, which may include interactive user authentication flows, token acquisition, and API access. This configuration would typically include details such as the application (client) ID, tenant ID, authority, redirect URI, and any necessary secrets or certificates required for client application authentication and authorization scenarios.
        /// </summary>
        public const string AppPCAClient = "App-PCAClient-Config";
        /// <summary>
        /// Represents the configuration key for the application's Web API settings.
        /// </summary>
        public const string AppWebApi = "App-WebApi-Config";
        //  - More specific test scenarios, edge cases, etc.
        /// <summary>
        /// Represents the application identifier used for B2C test scenarios in the MSAL App B2C JSON configuration.
        /// </summary>
        public const string B2CAppIdLabsAppB2C = "MSAL-App-B2C-JSON";
        /// <summary>
        /// Represents the application identifier used for Arlington test scenarios in the MSAL App Arlington JSON configuration.
        /// </summary>
        public const string ArlAppIdLabsApp = "MSAL-App-Arlington-JSON";
        /// <summary>
        /// Represents the identifier for the MSAL App CIAM JSON configuration format.
        /// </summary>
        public const string MsalAppCiam = "MSAL-App-CIAM-JSON";
        /// <summary>
        /// multiple orgs app in public cloud, with regional authority. Used for testing regional authority discovery and token acquisition in multi-tenant scenarios.
        /// </summary>
        public const string MsalAppAzureAdMultipleOrgsRegional = "MSAL-APP-AzureADMultipleOrgsRegional-JSON";
        /// <summary>
        /// Represents the application identifier used for Arlington CCA test scenarios in the MSAL App Arlington CCA JSON configuration.
        /// </summary>
        public const string MsalAppArlingtonCCA = "MSAL-App-ArlingtonCCA-JSON";

        // Name of key vault secrets for app secrets and certificates
        /// <summary>
        /// default secret value used for testing application authentication scenarios that require a client secret. This secret is typically associated with a test application registered in Azure Active Directory and is used to validate authentication flows that involve client credentials or other secret-based authentication methods.
        /// </summary>
        public const string DefaultAppSecret = "MSAL-App-Default";
    }
}

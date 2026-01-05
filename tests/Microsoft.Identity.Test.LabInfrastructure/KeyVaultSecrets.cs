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
        // User secrets
        public const string UserPublicCloud = "User-PublicCloud-Config";
        public const string UserFederated = "User-Federated-Config";
        public const string UserPublicCloud2 = "MSAL-User-Default2-JSON";
        public const string UserXcg = "MSAL-User-XCG-JSON";
        public const string UserB2C = "MSAL-USER-B2C-JSON";
        public const string UserArlington = "MSAL-USER-Arlington-JSON";
        public const string UserCiam = "MSAL-USER-CIAM-JSON";
        public const string UserPop = "MSAL-User-POP-JSON";

        // Lab environment secrets
        public const string Id4sLab1 = "ID4SLAB1";
        public const string MsidLabB2C = "MSIDLABB2C";
        public const string ArlMsidLab1 = "ARLMSIDLAB1";
        public const string MsidLabCiam6 = "MSIDLABCIAM6";

        // App secrets
        public const string AppPCAClient = "App-PCAClient-Config";
        public const string MsalAppAzureAdMultipleOrgs = "MSAL-APP-AzureADMultipleOrgs-JSON";
        public const string MsalAppAzureAdMultipleOrgsPublicClient = "MSAL-APP-AzureADMultipleOrgsPC-JSON";
        public const string B2CAppIdLabsAppB2C = "MSAL-App-B2C-JSON";
        public const string ArlAppIdLabsApp = "MSAL-App-Arlington-JSON";
        public const string MsalAppCiam = "MSAL-App-CIAM-JSON";

        // Other configuration secrets
        public const string EnvVariablesMsiConfig = "EnvVariables-MSI-Config";
    }
}

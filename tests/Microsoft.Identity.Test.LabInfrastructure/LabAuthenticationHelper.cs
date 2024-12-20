// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using System.Runtime.InteropServices;
using Azure.Identity.Broker;
using System.Diagnostics;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    internal static class LabAuthenticationHelper
    {
        public static async Task<AccessToken> GetAccessTokenForLabAPIAsync()
        {
            var tokenCredential = GetTokenCredential();

            return await tokenCredential
                           .GetTokenAsync(new TokenRequestContext([LabApiConstants.LabScope]), default)
                           .ConfigureAwait(false);
        }

        public static async Task<AccessToken> GetKeyVaultAccessToken()
        {
            var tokenCredential = GetTokenCredential();

            return await tokenCredential
                            .GetTokenAsync(new TokenRequestContext(["https://vault.azure.net/.default"]), default)
                            .ConfigureAwait(false);
        }

        internal static TokenCredential GetTokenCredential()
        {
            TokenCredential tokenCredential;
            if (Environment.GetEnvironmentVariable("TF_BUILD") == null)
            {
                Debug.WriteLine("[LabAPI] Not on CI, using interactive browser/broker credential");
                tokenCredential = GetAzureCredentialForDevBox();
            }
            else
            {
                Debug.WriteLine("[LabAPI] On CI, using ADO federation");
                tokenCredential = GetAzureCredentialForCI();
            }

            return tokenCredential;
        }

        private static TokenCredential GetAzureCredentialForCI()
        {
            // as per https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/samples/OtherCredentialSamples.md#authenticating-in-azure-pipelines-with-service-connections

            string clientId = "4b7a4b0b-ecb2-409e-879a-1e21a15ddaf6"; // UAMI client ID
            string tenantId = LabApiConstants.LabClientTenantId;
            string serviceConnectionId = "6eeeb73d-37aa-4d78-83b7-728101b8bddd";

            var pipelinesCredential = new AzurePipelinesCredential(
                tenantId,
                clientId,
                serviceConnectionId,
                Environment.GetEnvironmentVariable("SYSTEM_ACCESSTOKEN"),
                new AzurePipelinesCredentialOptions()
                {
                    TokenCachePersistenceOptions = new TokenCachePersistenceOptions()
                    {
                        Name = "MSIDLabTokenCache",
                        UnsafeAllowUnencryptedStorage = true // We generally use headless Linux, so cannot use LibSecret. This is the ~same level of protection as SSH keys.
                    }
                });

            return pipelinesCredential;
        }

        // TODO: test this on MacOs / Linux WSL
        private static TokenCredential GetAzureCredentialForDevBox()
        {
            InteractiveBrowserCredential interactiveBrowserCredential = new InteractiveBrowserCredential(
            new InteractiveBrowserCredentialBrokerOptions(GetForegroundWindow())
            {
                ClientId = LabApiConstants.LabClientId,
                TenantId = LabApiConstants.LabClientTenantId,
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions()
                {
                    Name = "MSIDLabTokenCache",
                    UnsafeAllowUnencryptedStorage = true // We generally use headless Linux, so cannot use LibSecret. This is the ~same level of protection as SSH keys.
                },
                RedirectUri = new Uri("http://localhost"), // On Mac and Linux, MSAL will fallback to browser
                UseDefaultBrokerAccount = true // 

            });

            return interactiveBrowserCredential;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
    }
}

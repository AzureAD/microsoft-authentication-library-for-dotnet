// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// ManagedIdentityTestUtil provides utility methods for setting up and managing environment variables
    /// related to managed identities in test scenarios.
    /// </summary>
    public static class ManagedIdentityTestUtil
    {
        /// <summary>
        /// User assigned identity identifier types for testing.
        /// </summary>
        public enum UserAssignedIdentityId
        {
            /// <summary>No user-assigned identity.</summary>
            None,
            /// <summary>Identified by client ID.</summary>
            ClientId,
            /// <summary>Identified by resource ID.</summary>
            ResourceId,
            /// <summary>Identified by object ID.</summary>
            ObjectId
        }

        /// <summary>
        /// MSI Azure resource types for testing.
        /// </summary>
        public enum MsiAzureResource
        {
            /// <summary>Azure Web App.</summary>
            WebApp,
            /// <summary>Azure Function.</summary>
            Function,
            /// <summary>Azure Virtual Machine.</summary>
            VM,
            /// <summary>Azure Arc.</summary>
            AzureArc,
            /// <summary>Azure Cloud Shell.</summary>
            CloudShell,
            /// <summary>Azure Service Fabric.</summary>
            ServiceFabric
        }

        /// <summary>
        /// Sets environment variables for the specified managed identity source.
        /// </summary>
        public static void SetEnvironmentVariables(ManagedIdentitySource managedIdentitySource, string endpoint, string secret = "secret", string thumbprint = "thumbprint")
        {
            switch (managedIdentitySource)
            {
                case ManagedIdentitySource.AppService:
                    Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", endpoint);
                    Environment.SetEnvironmentVariable("IDENTITY_HEADER", secret);
                    break;

                case ManagedIdentitySource.Imds:
                case ManagedIdentitySource.ImdsV2:
                    Environment.SetEnvironmentVariable("AZURE_POD_IDENTITY_AUTHORITY_HOST", endpoint);
                    break;

                case ManagedIdentitySource.AzureArc:
                    Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", endpoint);
                    Environment.SetEnvironmentVariable("IMDS_ENDPOINT", "http://localhost:40342");
                    break;

                case ManagedIdentitySource.CloudShell:
                    Environment.SetEnvironmentVariable("MSI_ENDPOINT", endpoint);
                    break;

                case ManagedIdentitySource.ServiceFabric:
                    Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", endpoint);
                    Environment.SetEnvironmentVariable("IDENTITY_HEADER", secret);
                    Environment.SetEnvironmentVariable("IDENTITY_SERVER_THUMBPRINT", thumbprint);
                    break;

                case ManagedIdentitySource.MachineLearning:
                    Environment.SetEnvironmentVariable("MSI_ENDPOINT", endpoint);
                    Environment.SetEnvironmentVariable("MSI_SECRET", secret);
                    Environment.SetEnvironmentVariable("DEFAULT_IDENTITY_CLIENT_ID", "fake_DEFAULT_IDENTITY_CLIENT_ID");
                    break;

                default:
                    throw new NotImplementedException($"Setting environment variables for {managedIdentitySource} is not implemented.");
            }
        }

        /// <summary>
        /// Sets environment variables for testing upgrade scenarios.
        /// </summary>
        internal static void SetUpgradeScenarioEnvironmentVariables(ManagedIdentitySource managedIdentitySource, string endpoint, string secret = "secret", string thumbprint = "thumbprint")
        {
            SetEnvironmentVariables(managedIdentitySource, endpoint, secret, thumbprint);

            switch (managedIdentitySource)
            {
                case ManagedIdentitySource.AppService:
                    Environment.SetEnvironmentVariable("MSI_ENDPOINT", endpoint);
                    Environment.SetEnvironmentVariable("MSI_SECRET", secret);
                    break;
            }
        }

        /// <summary>
        /// Create the MIA with the http proxy
        /// </summary>
        public static ManagedIdentityApplicationBuilder CreateMIABuilder(string userAssignedId = "", UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.ClientId)
        {
            var builder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedClientId(userAssignedId));

            switch (userAssignedIdentityId)
            {
                case UserAssignedIdentityId.ResourceId:
                    builder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedResourceId(userAssignedId));
                    break;

                case UserAssignedIdentityId.ObjectId:
                    builder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedObjectId(userAssignedId));
                    break;
            }

            builder.Config.AccessorOptions = null;

            return builder;
        }
    }
}

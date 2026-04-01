// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Lab.Api.Core.Mocks
{
    /// <summary>
    /// ManagedIdentityTestUtil provides utility methods for setting up and managing environment variables
    /// related to managed identities in test scenarios. These utilities help simulate different managed
    /// identity sources and configurations, facilitating comprehensive testing of MSAL's managed identity
    /// integration.
    /// </summary>
    public static class ManagedIdentityTestUtil
    {
        /// <summary>
        /// user assigned identity identifier types for testing. This enum is used to specify the type of identifier (ClientId, ResourceId, ObjectId) when creating a user-assigned managed identity in tests.
        /// </summary>
        public enum UserAssignedIdentityId
        {
            /// <summary>
            /// represents the absence of a user-assigned identity identifier. This value is used when creating a managed identity without specifying any identifier, allowing tests to verify behavior when no user-assigned identity is configured.
            /// </summary>
            None,
            /// <summary>
            /// represents a user-assigned identity identified by its client ID. This value is used when creating a managed identity with a specific client ID, allowing tests to verify behavior when a client ID is provided.
            /// </summary>  
            ClientId,
            /// <summary>
            /// represents a user-assigned identity identified by its resource ID. This value is used when creating a managed identity with a specific resource ID, allowing tests to verify behavior when a resource ID is provided.    
            /// </summary>
            ResourceId,
            /// <summary>
            /// represents a user-assigned identity identified by its object ID. This value is used when creating a managed identity with a specific object ID, allowing tests to verify behavior when an object ID is provided.
            /// </summary>
            ObjectId
        }

        //MSI Azure resources
        /// <summary>
        /// MSI Azure resource types for testing. This enum is used to specify the type of Azure resource (WebApp, Function, VM, AzureArc, CloudShell, ServiceFabric) when creating a managed identity in tests.
        /// </summary>
        public enum MsiAzureResource
        {
            /// <summary>
            /// Web App resource type for managed identity testing. This value is used when simulating a managed identity associated with an Azure Web App, allowing tests to verify behavior specific to this resource type.
            /// </summary>
            WebApp,
            /// <summary>
            /// Function resource type for managed identity testing. This value is used when simulating a managed identity associated with an Azure Function, allowing tests to verify behavior specific to this resource type.    
            /// </summary>
            Function,
            /// <summary>
            /// VM resource type for managed identity testing. This value is used when simulating a managed identity associated with an Azure Virtual Machine, allowing tests to verify behavior specific to this resource type.
            /// </summary>
            VM,
            /// <summary>
            /// Azure Arc resource type for managed identity testing. This value is used when simulating a managed identity associated with Azure Arc, allowing tests to verify behavior specific to this resource type.
            /// </summary>
            AzureArc,
            /// <summary>
            /// Cloud Shell resource type for managed identity testing. This value is used when simulating a managed identity associated with Azure Cloud Shell, allowing tests to verify behavior specific to this resource type.
            /// </summary>
            CloudShell,
            /// <summary>
            /// Service Fabric resource type for managed identity testing. This value is used when simulating a managed identity associated with Azure Service Fabric, allowing tests to verify behavior specific to this resource type.
            /// </summary>
            ServiceFabric
        }

        /// <summary>
        /// Sets environment variables for the specified managed identity source.
        /// </summary>
        /// <param name="managedIdentitySource">The managed identity source.</param>
        /// <param name="endpoint">The endpoint URL.</param>
        /// <param name="secret">The secret value (default is "secret").</param>
        /// <param name="thumbprint">The certificate thumbprint (default is "thumbprint").</param>
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
        /// This method mimics a scenario where older environment variables
        /// (e.g., MSI_ENDPOINT and MSI_SECRET) from previous versions of
        /// App Service (2017) still exist after an upgrade to newer versions (2019).
        /// It ensures that MSAL's Managed Identity source detection can correctly
        /// handle both legacy and new variables.
        /// </summary>
        /// <param name="managedIdentitySource">
        /// The type of managed identity source being tested (e.g., AppService, MachineLearning).
        /// </param>
        /// <param name="endpoint">
        /// The endpoint URL to be set as part of the environment variables.
        /// </param>
        /// <param name="secret">
        /// Optional: The secret value to be set (default is "secret").
        /// </param>
        /// <param name="thumbprint">
        /// Optional: The certificate thumbprint to be set (default is "thumbprint").
        /// </param>
        internal static void SetUpgradeScenarioEnvironmentVariables(ManagedIdentitySource managedIdentitySource, string endpoint, string secret = "secret", string thumbprint = "thumbprint")
        {
            // Use the common method to set base environment variables
            SetEnvironmentVariables(managedIdentitySource, endpoint, secret, thumbprint);

            // Add upgrade-specific variables where needed
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
        /// <param name="userAssignedId"></param>
        /// <param name="userAssignedIdentityId"></param>
        /// <returns></returns>
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

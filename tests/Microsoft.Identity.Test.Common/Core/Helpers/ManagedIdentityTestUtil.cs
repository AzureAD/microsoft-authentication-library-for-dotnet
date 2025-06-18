﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common.Core.Mocks;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public static class ManagedIdentityTestUtil
    {
        public enum UserAssignedIdentityId
        {
            None,
            ClientId,
            ResourceId,
            ObjectId
        }

        //MSI Azure resources
        public enum MsiAzureResource
        {
            WebApp,
            Function,
            VM,
            AzureArc,
            CloudShell,
            ServiceFabric
        }

        public static void SetEnvironmentVariables(ManagedIdentitySource managedIdentitySource, string endpoint, string secret = "secret", string thumbprint = "thumbprint")
        {
            switch (managedIdentitySource)
            {
                case ManagedIdentitySource.AppService:
                    Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", endpoint);
                    Environment.SetEnvironmentVariable("IDENTITY_HEADER", secret);
                    break;

                case ManagedIdentitySource.Imds:
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
        /// <param name="url"></param>
        /// <param name="userAssignedId"></param>
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

            // Disabling shared cache options to avoid cross test pollution.
            builder.Config.AccessorOptions = null;

            return builder;
        }
    }
}

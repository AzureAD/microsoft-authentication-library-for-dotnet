// Copyright (c) Microsoft Corporation. All rights reserved.
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

        public static void SetEnvironmentVariables(ManagedIdentitySource managedIdentitySource, string endpoint, string secret = "secret")
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
                    Environment.SetEnvironmentVariable("IDENTITY_SERVER_THUMBPRINT", "thumbprint");
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

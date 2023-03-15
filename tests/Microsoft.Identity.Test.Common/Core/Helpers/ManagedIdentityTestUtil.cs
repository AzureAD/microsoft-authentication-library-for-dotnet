// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public static class ManagedIdentityTestUtil
    {
        public enum UserAssignedIdentityId
        {
            None,
            ClientId,
            ResourceId
        }

        public enum ManagedIdentitySourceType
        {
            IMDS,
            AppService,
            AzureArc,
            CloudShell,
            ServiceFabric
        }

        public static void SetEnvironmentVariables(ManagedIdentitySourceType managedIdentitySource, string endpoint, string secret = "secret")
        {
            switch (managedIdentitySource)
            {
                case ManagedIdentitySourceType.AppService:
                    Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", endpoint);
                    Environment.SetEnvironmentVariable("IDENTITY_HEADER", secret);
                    break;

                case ManagedIdentitySourceType.IMDS:
                    Environment.SetEnvironmentVariable("AZURE_POD_IDENTITY_AUTHORITY_HOST", endpoint);
                    break;

                case ManagedIdentitySourceType.AzureArc:
                    Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", endpoint);
                    Environment.SetEnvironmentVariable("IMDS_ENDPOINT", "http://localhost:40342");
                    break;

                case ManagedIdentitySourceType.CloudShell:
                    Environment.SetEnvironmentVariable("MSI_ENDPOINT", endpoint);
                    break;

                case ManagedIdentitySourceType.ServiceFabric:
                    Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", endpoint);
                    Environment.SetEnvironmentVariable("IDENTITY_HEADER", secret);
                    Environment.SetEnvironmentVariable("IDENTITY_SERVER_THUMBPRINT", "thumbprint");
                    break;
            }
        }
    }
}

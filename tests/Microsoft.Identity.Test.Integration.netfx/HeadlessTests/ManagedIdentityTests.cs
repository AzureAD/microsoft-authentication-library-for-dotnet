// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.ManagedIdentity;

using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class ManagedIdentityTests
    {
        private static readonly string s_msi_scopes = "https://management.azure.com";
        private static readonly string s_wrong_msi_scopes = "https://managements.azure.com";

        //http proxy base URL 
        private static readonly string s_baseURL = "https://service.msidlab.com/";

        //Shared User Assigned Client ID
        private const string UserAssignedClientID = "3b57c42c-3201-4295-ae27-d6baec5b7027";

        private const string UserAssignedObjectID = "9fc6a41b-e161-43ba-90ba-12f172141c23";

        //Non Existent User Assigned Client/Object ID 
        private const string SomeRandomGuid = "f07359bb-f4f6-4e3c-ba9f-ccdf48eb80ce";

        //Error Messages
        private const string UserAssignedIdDoesNotExist = "[Managed Identity] Error Message: " +
            "No User Assigned or Delegated Managed Identity found for specified ClientId/ResourceId/PrincipalId.";

        //Resource ID of the User Assigned Identity 
        private const string UamiResourceId = "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/" +
            "resourcegroups/MSAL_MSI/providers/Microsoft.ManagedIdentity/userAssignedIdentities/" +
            "MSAL_MSI_USERID";

        //non existent Resource ID of the User Assigned Identity 
        private const string Non_Existent_UamiResourceId = "/subscriptions/userAssignedIdentities/NO_ID";

        [DataTestMethod]
        [DataRow(MsiAzureResource.WebApp, "", DisplayName = "System_Identity_Web_App")]
        [DataRow(MsiAzureResource.Function, "", DisplayName = "System_Identity_Function_App")]
        [DataRow(MsiAzureResource.VM, "", DisplayName = "System_Identity_Virtual_Machine")]
        [DataRow(MsiAzureResource.WebApp, UserAssignedClientID, UserAssignedIdentityId.ClientId, DisplayName = "ClientId_Web_App")]
        [DataRow(MsiAzureResource.Function, UserAssignedClientID, UserAssignedIdentityId.ClientId, DisplayName = "ClientId_Function_App")]
        [DataRow(MsiAzureResource.VM, UserAssignedClientID, UserAssignedIdentityId.ClientId, DisplayName = "ClientId_Virtual_Machine")]
        [DataRow(MsiAzureResource.WebApp, UamiResourceId, UserAssignedIdentityId.ResourceId, DisplayName = "ResourceID_Web_App")]
        [DataRow(MsiAzureResource.Function, UamiResourceId, UserAssignedIdentityId.ResourceId, DisplayName = "ResourceID_Function_App")]
        [DataRow(MsiAzureResource.VM, UamiResourceId, UserAssignedIdentityId.ResourceId, DisplayName = "ResourceID_Virtual_Machine")]
        [DataRow(MsiAzureResource.WebApp, UserAssignedObjectID, UserAssignedIdentityId.ObjectId, DisplayName = "ObjectID_Web_App")]
        [DataRow(MsiAzureResource.Function, UserAssignedObjectID, UserAssignedIdentityId.ObjectId, DisplayName = "ObjectID_Function_App")]
        [DataRow(MsiAzureResource.VM, UserAssignedObjectID, UserAssignedIdentityId.ObjectId, DisplayName = "ObjectID_Virtual_Machine")]
#if ENABLE_AZURE_ARC_TESTS
        [DataRow(MsiAzureResource.AzureArc, "", DisplayName = "Azure_ARC")]
#endif
        public async Task AcquireMSITokenAsync(MsiAzureResource azureResource, string userIdentity, UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None)
        {
            //Arrange
            using (new EnvVariableContext())
            {
                // Fetch the env variables from the resource and set them locally
                Dictionary<string, string> envVariables = 
                    await GetEnvironmentVariablesAsync(azureResource).ConfigureAwait(false);

                //Set the Environment Variables
                SetEnvironmentVariables(envVariables);

                //form the http proxy URI 
                string uri = s_baseURL + $"MSIToken?" +
                    $"azureresource={azureResource}&uri=";

                //Create CCA with Proxy
                IManagedIdentityApplication mia = CreateMIAWithProxy(uri, userIdentity, userAssignedIdentityId);

                AuthenticationResult result;
                //Act
                result = await mia
                            .AcquireTokenForManagedIdentity(s_msi_scopes)
                            .ExecuteAsync().ConfigureAwait(false);

                //Assert
                //1. Token Type
                Assert.AreEqual("Bearer", result.TokenType);

                //2. First token response is from the MSI Endpoint
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                //3. Validate the ExpiresOn falls within a 24 hour range from now
                CoreAssert.IsWithinRange(
                                DateTimeOffset.UtcNow + TimeSpan.FromHours(0),
                                result.ExpiresOn,
                                TimeSpan.FromHours(24));

                result = await mia
                    .AcquireTokenForManagedIdentity(s_msi_scopes)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                //4. Validate the scope
                Assert.IsTrue(result.Scopes.All(s_msi_scopes.Contains));

                //5. Validate the second call to token endpoint gets returned from the cache
                Assert.AreEqual(TokenSource.Cache, 
                    result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(MsiAzureResource.WebApp, SomeRandomGuid, UserAssignedIdentityId.ClientId, DisplayName = "ClientId_Web_App")]
        [DataRow(MsiAzureResource.WebApp, SomeRandomGuid, UserAssignedIdentityId.ObjectId, DisplayName = "ObjectId_Web_App")]
        [DataRow(MsiAzureResource.WebApp, Non_Existent_UamiResourceId, UserAssignedIdentityId.ResourceId, DisplayName = "ResourceID_Web_App")]
        [DataRow(MsiAzureResource.Function, SomeRandomGuid, UserAssignedIdentityId.ClientId, DisplayName = "ClientId_Function_App")]
        [DataRow(MsiAzureResource.Function, SomeRandomGuid, UserAssignedIdentityId.ObjectId, DisplayName = "ObjectId_Function_App")]
        [DataRow(MsiAzureResource.Function, Non_Existent_UamiResourceId, UserAssignedIdentityId.ResourceId, DisplayName = "ResourceID_Function_App")]
        //Disabling VM tests for now becasue this needs a change to the helper service
        //[DataRow(MsiAzureResource.VM, SomeRandomGuid, UserAssignedIdentityId.ClientId, DisplayName = "ClientId_VM")]
        //[DataRow(MsiAzureResource.VM, SomeRandomGuid, UserAssignedIdentityId.ObjectId, DisplayName = "ObjectId_VM")]
        //[DataRow(MsiAzureResource.VM, Non_Existent_UamiResourceId, UserAssignedIdentityId.ResourceId, DisplayName = "ResourceID_VM")]
        public async Task ManagedIdentityRequestFailureCheckAsync(MsiAzureResource azureResource, string userIdentity, UserAssignedIdentityId userAssignedIdentityId)
        {
            //Arrange
            using (new EnvVariableContext())
            {
                //Get the Environment Variables
                Dictionary<string, string> envVariables =
                    await GetEnvironmentVariablesAsync(azureResource).ConfigureAwait(false);

                //Set the Environment Variables
                SetEnvironmentVariables(envVariables);

                //form the http proxy URI 
                string uri = s_baseURL + $"MSIToken?" +
                    $"azureresource={azureResource}&uri=";

                //Create ManagedIdentityApplication with Proxy
                IManagedIdentityApplication mia = CreateMIAWithProxy(uri, userIdentity, userAssignedIdentityId);

                //Act
                MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(async () =>
                {
                    await mia
                    .AcquireTokenForManagedIdentity(s_msi_scopes)
                    .ExecuteAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);

                //set the expected resource
                ManagedIdentitySource expectedResource = azureResource == MsiAzureResource.VM ?
                    ManagedIdentitySource.Imds : ManagedIdentitySource.AppService;

                //Assert
                Assert.IsTrue(ex.ErrorCode == MsalError.ManagedIdentityRequestFailed);
                Assert.AreEqual(expectedResource.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
            }
        }

        [DataTestMethod]
        [DataRow(MsiAzureResource.WebApp, "", UserAssignedIdentityId.None, DisplayName = "System_Identity_Web_App")]
        [DataRow(MsiAzureResource.WebApp, UserAssignedClientID, UserAssignedIdentityId.ClientId, DisplayName = "ClientId_Web_App")]
        [DataRow(MsiAzureResource.WebApp, UamiResourceId, UserAssignedIdentityId.ResourceId, DisplayName = "ResourceId_Web_App")]
        [DataRow(MsiAzureResource.WebApp, UserAssignedObjectID, UserAssignedIdentityId.ObjectId, DisplayName = "ObjectId_Web_App")]
        public async Task MSIWrongScopesAsync(MsiAzureResource azureResource, string userIdentity, UserAssignedIdentityId userAssignedIdentityId)
        {
            //Arrange
            using (new EnvVariableContext())
            {
                //Get the Environment Variables
                Dictionary<string, string> envVariables =
                    await GetEnvironmentVariablesAsync(azureResource).ConfigureAwait(false);

                //Set the Environment Variables
                SetEnvironmentVariables(envVariables);

                //form the http proxy URI 
                string uri = s_baseURL + $"MSIToken?" +
                    $"azureresource={azureResource}&uri=";

                //Create CCA with Proxy
                IManagedIdentityApplication mia = CreateMIAWithProxy(uri, userIdentity, userAssignedIdentityId);

                //Act
                MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(async () =>
                {
                    await mia
                    .AcquireTokenForManagedIdentity(s_wrong_msi_scopes)
                    .ExecuteAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);

                //Assert
                Assert.IsTrue(ex.ErrorCode == MsalError.ManagedIdentityRequestFailed);
                Assert.AreEqual(ManagedIdentitySource.AppService.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
            }
        }

        /// <summary>
        /// Gets the environment variable
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        private async Task<Dictionary<string, string>> GetEnvironmentVariablesAsync(
            MsiAzureResource resource)
        {
            Dictionary<string, string> environmentVariables = new Dictionary<string, string>();

            //Get the Environment Variables from the MSI Helper Service
            string uri = s_baseURL + "EnvironmentVariables?resource=" + resource;

            var environmentVariableResponse = await LabUserHelper
                .GetMSIEnvironmentVariablesAsync(uri)
                .ConfigureAwait(false);

            //process the response
            if (!string.IsNullOrEmpty(environmentVariableResponse))
            {
#if NET6_0_OR_GREATER
                environmentVariables = System.Text.Json.JsonSerializer.Deserialize
                    <Dictionary<string, string>>(environmentVariableResponse);
#else
                environmentVariables = Microsoft.Identity.Json.JsonConvert.DeserializeObject
                    <Dictionary<string, string>>(environmentVariableResponse);
#endif
            }

            return environmentVariables;
        }

        /// <summary>
        /// Sets the Environment Variables
        /// </summary>
        /// <param name="envVariables"></param>
        private void SetEnvironmentVariables(Dictionary<string, string> envVariables)
        {
            //Set the environment variables
            foreach (KeyValuePair<string, string> kvp in envVariables)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Create the MIA with the http proxy
        /// </summary>
        /// <param name="url"></param>
        /// <param name="userAssignedId"></param>
        /// <returns></returns>
        private IManagedIdentityApplication CreateMIAWithProxy(string url, string userAssignedId = "", UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None)
        {
            //Proxy the MSI token request 
            MsiProxyHttpManager proxyHttpManager = new MsiProxyHttpManager(url);

            var builder = ManagedIdentityApplicationBuilder
               .Create(ManagedIdentityId.SystemAssigned);

            switch (userAssignedIdentityId)
            {
                case UserAssignedIdentityId.ClientId: 
                    builder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedClientId(userAssignedId));
                    break;

                case UserAssignedIdentityId.ResourceId:
                    builder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedResourceId(userAssignedId));
                    break;

                case UserAssignedIdentityId.ObjectId:
                    builder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedObjectId(userAssignedId));
                    break;
            }

            // Disabling shared cache options to avoid cross test pollution.
            builder.Config.AccessorOptions = null;

            IManagedIdentityApplication mia = builder
                .WithHttpManager(proxyHttpManager).Build();

            return mia;
        }
    }
}

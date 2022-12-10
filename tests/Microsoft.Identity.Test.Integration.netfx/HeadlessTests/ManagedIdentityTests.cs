// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Json;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute.Exceptions;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class ManagedIdentityTests
    {
        private static readonly string[] s_msi_scopes = { "https://management.azure.com" };
        private static readonly string s_baseURL = "https://service.msidlab.com/";
        private static readonly string s_clientId = "client_id";
        private static Dictionary<string, string> s_envVariables = new Dictionary<string, string>();
        private const string UserAssignedClientID = "3b57c42c-3201-4295-ae27-d6baec5b7027";
        private const string Mi_res_id = "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSAL_MSI/providers/Microsoft.ManagedIdentity/userAssignedIdentities/MSAL_MSI_USERID";

        public enum AzureResource
        {
            webapp,
            function,
            vm,
            azurearc,
            cloudshell,
            servicefabric
        }

        [TestMethod]
        public async Task ManagedIdentitySourceCheckAsync()
        {
            //Arrange
            string result = string.Empty;
            string exception = string.Empty;
            string expectedClientException = "Authentication with managed identity is unavailable. " +
                "No managed identity endpoint found.";

            IConfidentialClientApplication cca = CreateCCAWithProxy(s_baseURL);

            //Act
            try
            {
                AuthenticationResult authenticationResult = await cca.AcquireTokenForClient(s_msi_scopes)
                    .WithManagedIdentity()
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalClientException ex)
            {
                result = ex.Message;
            }
            catch (Exception ex)
            {
                exception = ex.Message;
            }

            //Assert
            Assert.IsTrue(string.IsNullOrEmpty(exception));
            Assert.AreSame(result, expectedClientException);
        }

        [DataTestMethod]
        [DataRow(AzureResource.webapp, "", DisplayName = "System Identity Web App")]
        [DataRow(AzureResource.function, "", DisplayName = "System Identity Function App")]
        [DataRow(AzureResource.webapp, UserAssignedClientID, DisplayName = "User Identity Web App")]
        [DataRow(AzureResource.function, UserAssignedClientID, DisplayName = "User Identity Function App")]
        [DataRow(AzureResource.webapp, Mi_res_id, DisplayName = "ResourceID Web App")]
        [DataRow(AzureResource.function, Mi_res_id, DisplayName = "ResourceID Function App")]
        public async Task AcquireMSITokenAsync(AzureResource azureResource, string userIdentity)
        {
            //Arrange
            AuthenticationResult result = null;

            //Set the Environment Variables
            bool isEnvironmentVariableSet = await SetEnvironmentVariablesAsync(azureResource)
                .ConfigureAwait(false);

            if (!isEnvironmentVariableSet)
            {
                Assert.Fail("AcquireMSITokenForWebAppAsync failed to set environment variables.");
            }

            //form the URI 
            string uri = s_baseURL + $"GetMSIToken?azureresource={azureResource}&uri=";

            //Create CCA with Proxy
            IConfidentialClientApplication cca = CreateCCAWithProxy(uri);

            //Act
            try
            {
                result = await cca.AcquireTokenForClient(s_msi_scopes)
                    .WithManagedIdentity(userIdentity)
                    .ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                Assert.IsNull(ex.Message);
            }

            //Assert
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            //User Identity Client ID 
            //Need to check if we are exposing Client ID in msal token response
            //if (!string.IsNullOrEmpty(userIdentity))
            //{
            //    Assert.AreEqual(UserAssignedClientID, result.);
            //}

            result = await cca.AcquireTokenForClient(s_msi_scopes)
                .WithManagedIdentity(userIdentity)
                .ExecuteAsync().ConfigureAwait(false);

            Assert.IsNotNull(result.Scopes);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            ClearEnvironmentVariables();

        }

        private async Task<bool> SetEnvironmentVariablesAsync(AzureResource resource)
        {
            //Get the Web App Environment Variables from the MSI Helper Service
            string uri = s_baseURL + "GetEnvironmentVariables?resource=" + resource;

            var environmentVariableResponse = await LabUserHelper
                .GetMSIEnvironmentVariablesAsync(uri) 
                .ConfigureAwait(false);

            //process the response
            if (!string.IsNullOrEmpty(environmentVariableResponse))
            {
                s_envVariables = JsonConvert.DeserializeObject<Dictionary<string, string>>(environmentVariableResponse);
            }
            else
            {
                return false;
            }

            //Set the environment variables
            foreach (KeyValuePair<string, string> kvp in s_envVariables)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }

            return true;
        }

        private void ClearEnvironmentVariables()
        {
            //Clear the environment variables
            foreach (KeyValuePair<string, string> kvp in s_envVariables)
            {
                Environment.SetEnvironmentVariable(kvp.Key, "");
            }
        }

        private IConfidentialClientApplication CreateCCAWithProxy(string url)
        {
            //Proxy the request 
            ProxyHttpManager proxyHttpManager = new ProxyHttpManager(url);

            ConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
               .Create(s_clientId)
               .WithExperimentalFeatures()
               .WithHttpManager(proxyHttpManager)
               .BuildConcrete();

            return cca;
        }
    }
}

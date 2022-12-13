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
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Json;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
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
        private static readonly string s_errorMessageWithCorrId = "[Managed Identity] Error message:  " +
            "Correlation Id: ";
        private static readonly string s_emptyResponse = "[Managed Identity] Empty error response received.";
        private const string UserAssignedClientID = "3b57c42c-3201-4295-ae27-d6baec5b7027";
        private const string Mi_res_id = "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSAL_MSI/providers/Microsoft.ManagedIdentity/userAssignedIdentities/MSAL_MSI_USERID";

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
        [DataRow(MsiAzureResource.WebApp, "", DisplayName = "System Identity Web App")]
        [DataRow(MsiAzureResource.Function, "", DisplayName = "System Identity Function App")]
        [DataRow(MsiAzureResource.WebApp, UserAssignedClientID, DisplayName = "User Identity Web App")]
        [DataRow(MsiAzureResource.Function, UserAssignedClientID, DisplayName = "User Identity Function App")]
        [DataRow(MsiAzureResource.WebApp, Mi_res_id, DisplayName = "ResourceID Web App")]
        [DataRow(MsiAzureResource.Function, Mi_res_id, DisplayName = "ResourceID Function App")]
        public async Task AcquireMSITokenAsync(MsiAzureResource azureResource, string userIdentity)
        {
            //Arrange
            AuthenticationResult result;

            //Set the Environment Variables
            var environmentVariables = await GetEnvironmentVariablesAsync(azureResource)
                .ConfigureAwait(false);

            SetEnvironmentVariables(environmentVariables);

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
            catch (MsalClientException ex)
            {
                throw new Exception(ex.Message);
            }

            //Assert
            Assert.IsNotNull(result.AccessToken);

            CoreAssert.IsWithinRange(
                            DateTimeOffset.UtcNow + TimeSpan.FromHours(0),
                            result.ExpiresOn,
                            TimeSpan.FromHours(24));

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            result = await cca.AcquireTokenForClient(s_msi_scopes)
                .WithManagedIdentity(userIdentity)
                .ExecuteAsync().ConfigureAwait(false);

            Assert.IsNotNull(result.Scopes);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            ClearEnvironmentVariables(environmentVariables);

        }

        //[DataTestMethod]
        //[DataRow(MsiAzureResource.WebApp, "", SendMSIHeader.None, DisplayName = "Web App No Header")]
        //[DataRow(MsiAzureResource.WebApp, "", SendMSIHeader.WithWrongValue, DisplayName = "Web App Wrong Header")]
        public async Task TryAcquireMSITokenHeaderTestAsync(
            MsiAzureResource azureResource, 
            string userIdentity,
            SendMSIHeader sendHeader)
        {
            //Arrange

            //Set the Environment Variables
            var environmentVariables = await GetEnvironmentVariablesAsync(azureResource)
                .ConfigureAwait(false);

            SetEnvironmentVariables(environmentVariables);

            //form the URI 
            string uri = s_baseURL + $"GetMSIToken?azureresource={azureResource}&uri=";

            //Create CCA with Proxy
            IConfidentialClientApplication cca = CreateCCAWithProxy(uri, sendHeader: sendHeader);

            //Act
            try
            {
                AuthenticationResult result = await cca.AcquireTokenForClient(s_msi_scopes)
                    .WithManagedIdentity(userIdentity)
                    .ExecuteAsync().ConfigureAwait(false);
            }
            catch (MsalServiceException ex)
            {
                string messageToCheck = sendHeader == SendMSIHeader.None ? 
                    s_errorMessageWithCorrId : s_emptyResponse;

                //When we do not send headers in the request, APP service will reply back with 
                //{"type":"https://tools.ietf.org/html/rfc7231#section-6.5.1",
                //"title":"One or more validation errors occurred.",
                //"status":400,"traceId":"00-b378055402d5a88a840d248f138dc7f3-3377be9e34b55a52-00",
                //"errors":{"X-IDENTITY-HEADER":["The identityHeader field is required."]}}
                //This may not be a valid case becaue we identify the app service based on the 
                //header environment variable. so the value cannot be stripped but could there be a 
                //scenario where this could happen or should we handle exceptions better in this scenario
                Assert.AreEqual(messageToCheck, ex.Message);
            }

            ClearEnvironmentVariables(environmentVariables);
        }

        //[DataTestMethod]
        //[DataRow(MsiAzureResource.WebApp, "", MSIResource.None, DisplayName = "Web App No Resource")]
        //[DataRow(MsiAzureResource.WebApp, "", MSIResource.Fake, DisplayName = "Web App Wrong Resource")]
        public async Task TryAcquireMSITokenResourceTestAsync(
            MsiAzureResource azureResource,
            string userIdentity,
            MSIResource msiResource)
        {
            //Arrange

            //Set the Environment Variables
            var environmentVariables = await GetEnvironmentVariablesAsync(azureResource)
                .ConfigureAwait(false);

            SetEnvironmentVariables(environmentVariables);

            //form the URI 
            string uri = s_baseURL + $"GetMSIToken?azureresource={azureResource}&uri=";

            //Create CCA with Proxy
            IConfidentialClientApplication cca = CreateCCAWithProxy(uri, msiResource: msiResource);

            //Act
            try
            {
                AuthenticationResult result = await cca.AcquireTokenForClient(s_msi_scopes)
                    .WithManagedIdentity(userIdentity)
                    .ExecuteAsync().ConfigureAwait(false);
            }
            catch (MsalServiceException ex)
            {
                string messageToCheck = s_emptyResponse;

                //When we do not send resource in the request, APP service will reply back with 
                //"{\"targetSite\":null,\"message\":\"Query string missing resource.\",\"data\":{},
                //\"innerException\":null,\"helpLink\":null,\"source\":null,\"hResult\":-2146233088,\
                //"stackTrace\":null}"

                //And for the wrong resource MSI service actually gives back a token for https://bing.com

                Assert.AreEqual(messageToCheck, ex.Message);
            }

            ClearEnvironmentVariables(environmentVariables);
        }

        //[DataTestMethod]
        //[DataRow(MsiAzureResource.WebApp, "", MSIApiVersion.Fake, DisplayName = "Web App Wrong Api Version")]
        public async Task TryAcquireMSITokenApiVersionTestAsync(
            MsiAzureResource azureResource,
            string userIdentity,
            MSIApiVersion msiApiVersion)
        {
            //Arrange

            //Set the Environment Variables
            var environmentVariables = await GetEnvironmentVariablesAsync(azureResource)
                .ConfigureAwait(false);

            SetEnvironmentVariables(environmentVariables);

            //form the URI 
            string uri = s_baseURL + $"GetMSIToken?azureresource={azureResource}&uri=";

            //Create CCA with Proxy
            IConfidentialClientApplication cca = CreateCCAWithProxy(uri, msiApiVersion: msiApiVersion);

            //Act
            try
            {
                AuthenticationResult result = await cca.AcquireTokenForClient(s_msi_scopes)
                    .WithManagedIdentity(userIdentity)
                    .ExecuteAsync().ConfigureAwait(false);
            }
            catch (MsalServiceException ex)
            {
                string messageToCheck = s_emptyResponse;

                //When we  send a wrong API version in the request, APP service will reply back with 
                //"{\"error\":{\"code\":\"UnsupportedApiVersion\",\"message\":\
                //"The HTTP resource that matches the request URI 'http://127.0.0.1:41292/msi/token'
                //does not support the API version '2017-08-01'.\",\"innerError\":null}}"
                Assert.AreEqual(messageToCheck, ex.Message);
            }

            ClearEnvironmentVariables(environmentVariables);
        }

        private async Task<Dictionary<string,string>> GetEnvironmentVariablesAsync(MsiAzureResource resource)
        {
            try
            {
                //Get the Web App Environment Variables from the MSI Helper Service
                string uri = (s_baseURL + "GetEnvironmentVariables?resource=" + resource).ToLowerInvariant();

                var environmentVariableResponse = await LabUserHelper
                    .GetMSIEnvironmentVariablesAsync(uri)
                    .ConfigureAwait(false);

                //process the response
                Dictionary<string, string> environmentVariables = 
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(environmentVariableResponse);

                return environmentVariables;
            }
            catch
            {
                throw new Exception("Test Failure - Unable to get MSI Environment Variables. Check MSI Helper Service.");
            }
        }

        private void SetEnvironmentVariables(Dictionary<string, string> environmentVariables)
        {
            //Set the environment variables
            foreach (KeyValuePair<string, string> kvp in environmentVariables)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }

        private void ClearEnvironmentVariables(Dictionary<string, string> environmentVariables)
        {
            //Clear the environment variables
            foreach (KeyValuePair<string, string> kvp in environmentVariables)
            {
                Environment.SetEnvironmentVariable(kvp.Key, "");
            }
        }

        private IConfidentialClientApplication CreateCCAWithProxy(
            string url, 
            SendMSIHeader sendHeader = SendMSIHeader.Original,
            MSIResource msiResource = MSIResource.Original,
            MSIApiVersion msiApiVersion = MSIApiVersion.MsalDefault)
        {
            //Proxy the request 
            ProxyHttpManager proxyHttpManager = new ProxyHttpManager
                (url, sendHeader, msiResource, msiApiVersion);

            ConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
               .Create(s_clientId)
               .WithExperimentalFeatures()
               .WithHttpManager(proxyHttpManager)
               .BuildConcrete();

            return cca;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute.Core;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    [DeploymentItem("Resources\\ManagedIdentityAzureArcSecret.txt")]
    public class AzureArcTests : TestBase
    {
        private const string AzureArc = "Azure Arc";

        [TestMethod]
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow(TestConstants.MiResourceId, UserAssignedIdentityId.ResourceId)]
        [DataRow(TestConstants.ObjectId, UserAssignedIdentityId.ObjectId)]
        public async Task AzureArcUserAssignedManagedIdentityHappyPathAsync(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AzureArc, ManagedIdentityTests.AzureArcEndpoint);

                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(userAssignedId, userAssignedIdentityId);
                miBuilder.WithHttpManager(httpManager);

                IManagedIdentityApplication mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.AzureArcEndpoint,
                    ManagedIdentityTests.Resource,
                    GetArcUserAssignedSuccessResponse(userAssignedId, userAssignedIdentityId),
                    ManagedIdentitySource.AzureArc,
                    userAssignedId: userAssignedId,
                    userAssignedIdentityId: userAssignedIdentityId);

                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow(TestConstants.MiResourceId, UserAssignedIdentityId.ResourceId)]
        [DataRow(TestConstants.ObjectId, UserAssignedIdentityId.ObjectId)]
        public async Task AzureArcUserAssignedManagedIdentityNotHonoredFailsAsync(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AzureArc, ManagedIdentityTests.AzureArcEndpoint);

                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(userAssignedId, userAssignedIdentityId);
                miBuilder.WithHttpManager(httpManager);

                IManagedIdentityApplication mi = miBuilder.Build();

                // Simulate a legacy Azure Arc agent: it ignores the selector and returns a token that
                // does not confirm the requested user-assigned identity (no matching echo field).
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.AzureArcEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.AzureArc,
                    userAssignedId: userAssignedId,
                    userAssignedIdentityId: userAssignedIdentityId);

                MsalServiceException ex = await Assert.ThrowsAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.AzureArc.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.UserAssignedManagedIdentityNotSupported, ex.ErrorCode);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityUserAssignedNotSupported, AzureArc), ex.Message);
            }
        }

        // Builds an Azure Arc token response that echoes the requested user-assigned identity,
        // simulating an agent that honors the selector.
        private static string GetArcUserAssignedSuccessResponse(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId)
        {
            string echoedField;
            switch (userAssignedIdentityId)
            {
                case UserAssignedIdentityId.ClientId:
                    echoedField = "\"client_id\":\"" + userAssignedId + "\"";
                    break;
                case UserAssignedIdentityId.ResourceId:
                    echoedField = "\"msi_res_id\":\"" + userAssignedId + "\"";
                    break;
                case UserAssignedIdentityId.ObjectId:
                    echoedField = "\"object_id\":\"" + userAssignedId + "\"";
                    break;
                default:
                    echoedField = null;
                    break;
            }

            string expiresOn = ((long)(DateTime.UtcNow.AddHours(1) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);

            string identityPart = echoedField is null ? string.Empty : "," + echoedField;

            return "{\"access_token\":\"" + TestConstants.ATSecret + "\",\"expires_on\":\"" + expiresOn +
                   "\",\"resource\":\"https://management.azure.com/\",\"token_type\":\"Bearer\"" + identityPart + "}";
        }

        [TestMethod]
        public async Task AzureArcAuthHeaderMissingAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AzureArc, ManagedIdentityTests.AzureArcEndpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                
                

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityWSTrustMockHandler(ManagedIdentityTests.AzureArcEndpoint);

                MsalServiceException ex = await Assert.ThrowsAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.AzureArc.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityNoChallengeError, ex.Message);
            }
        }

        [TestMethod]
        [DataRow("somefile=filename", MsalErrorMessage.ManagedIdentityInvalidChallenge)]
        [DataRow("C:\\ProgramData\\AzureConnectedMachineAgent\\Tokens\\filename.txt", MsalErrorMessage.ManagedIdentityInvalidFile)]
        [DataRow("C:\\ProgramData\\AzureConnectedMachineAgent\\Tokens\\...\\etc\\filename.key", MsalErrorMessage.ManagedIdentityInvalidFile)]
        public async Task AzureArcAuthHeaderInvalidAsync(string filename, string errorMessage)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AzureArc, ManagedIdentityTests.AzureArcEndpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                
                

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityWSTrustMockHandler(ManagedIdentityTests.AzureArcEndpoint, filename);

                MsalServiceException ex = await Assert.ThrowsAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.AzureArc.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(errorMessage, ex.Message);
            }
        }

        [TestMethod]
        public async Task AzureArcInvalidEndpointAsync()
        {
            using(new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AzureArc, "localhost/token");

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                
                

                var mi = miBuilder.Build();

                MsalServiceException ex = await Assert.ThrowsAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.AzureArc.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.InvalidManagedIdentityEndpoint, ex.ErrorCode);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, "IDENTITY_ENDPOINT", "localhost/token", AzureArc), ex.Message);
            }
        }
    }
}

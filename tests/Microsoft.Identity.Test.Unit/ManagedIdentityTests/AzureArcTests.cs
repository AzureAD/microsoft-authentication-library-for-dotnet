// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.Internal;
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
        private static string GetArcUserAssignedSuccessResponse(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId, bool useArcResourceIdSpelling = false)
        {
            string echoedField;
            switch (userAssignedIdentityId)
            {
                case UserAssignedIdentityId.ClientId:
                    echoedField = "\"client_id\":\"" + userAssignedId + "\"";
                    break;
                case UserAssignedIdentityId.ResourceId:
                    echoedField = "\"" + (useArcResourceIdSpelling ? "mi_res_id" : "msi_res_id") + "\":\"" + userAssignedId + "\"";
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
        public async Task AzureArcUserAssignedManagedIdentityNotFoundSurfacesServiceErrorAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AzureArc, ManagedIdentityTests.AzureArcEndpoint);

                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(TestConstants.ClientId, UserAssignedIdentityId.ClientId);
                miBuilder.WithHttpManager(httpManager);

                IManagedIdentityApplication mi = miBuilder.Build();

                // A new Arc agent returns 404 when the requested user-assigned identity is not assigned
                // to the machine. This must surface as a service error, not the "not supported" error.
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.AzureArcEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiErrorResponse(ManagedIdentitySource.AzureArc),
                    ManagedIdentitySource.AzureArc,
                    userAssignedId: TestConstants.ClientId,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId,
                    statusCode: HttpStatusCode.NotFound);

                MsalServiceException ex = await Assert.ThrowsAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.AzureArc.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreNotEqual(MsalError.UserAssignedManagedIdentityNotSupported, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task AzureArcUserAssignedManagedIdentityDifferentIdentityEchoedFailsAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AzureArc, ManagedIdentityTests.AzureArcEndpoint);

                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(TestConstants.ClientId, UserAssignedIdentityId.ClientId);
                miBuilder.WithHttpManager(httpManager);

                IManagedIdentityApplication mi = miBuilder.Build();

                // The agent echoes a different client_id than the caller requested. This is distinct from
                // the "no echo field" case, and must still fail closed.
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.AzureArcEndpoint,
                    ManagedIdentityTests.Resource,
                    GetArcUserAssignedSuccessResponse(TestConstants.ObjectId, UserAssignedIdentityId.ClientId),
                    ManagedIdentitySource.AzureArc,
                    userAssignedId: TestConstants.ClientId,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId);

                MsalServiceException ex = await Assert.ThrowsAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.UserAssignedManagedIdentityNotSupported, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task AzureArcUserAssignedResourceIdEchoedAsMiResIdIsAcceptedAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AzureArc, ManagedIdentityTests.AzureArcEndpoint);

                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(TestConstants.MiResourceId, UserAssignedIdentityId.ResourceId);
                miBuilder.WithHttpManager(httpManager);

                IManagedIdentityApplication mi = miBuilder.Build();

                // The agent may echo the resource id under "mi_res_id" (the spelling MSAL sends on the
                // request) instead of the IMDS "msi_res_id". MSAL must accept either spelling.
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.AzureArcEndpoint,
                    ManagedIdentityTests.Resource,
                    GetArcUserAssignedSuccessResponse(TestConstants.MiResourceId, UserAssignedIdentityId.ResourceId, useArcResourceIdSpelling: true),
                    ManagedIdentitySource.AzureArc,
                    userAssignedId: TestConstants.MiResourceId,
                    userAssignedIdentityId: UserAssignedIdentityId.ResourceId);

                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow(TestConstants.MiResourceId, UserAssignedIdentityId.ResourceId)]
        [DataRow(TestConstants.ObjectId, UserAssignedIdentityId.ObjectId)]
        public async Task AzureArcUserAssignedManagedIdentityCacheIsPartitionedByIdentityAsync(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AzureArc, ManagedIdentityTests.AzureArcEndpoint);

                var uami = CreateMIABuilder(userAssignedId, userAssignedIdentityId)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // The cache is partitioned by the requested identity: a user-assigned request is keyed
                // by its own identity (client id / resource id / object id), not the system-assigned
                // default, so SAMI and each UAMI get separate cache entries.
                var recorder = uami.AppTokenCacheInternal.RecordAccess((args) =>
                {
                    Assert.AreEqual(userAssignedId, args.ClientId);
                    Assert.AreNotEqual(Constants.ManagedIdentityDefaultClientId, args.ClientId);
                });

                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.AzureArcEndpoint,
                    ManagedIdentityTests.Resource,
                    GetArcUserAssignedSuccessResponse(userAssignedId, userAssignedIdentityId),
                    ManagedIdentitySource.AzureArc,
                    userAssignedId: userAssignedId,
                    userAssignedIdentityId: userAssignedIdentityId);

                AuthenticationResult idp = await uami.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(TokenSource.IdentityProvider, idp.AuthenticationResultMetadata.TokenSource);

                AuthenticationResult cached = await uami.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);

                recorder.AssertAccessCounts(2, 1);
            }
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

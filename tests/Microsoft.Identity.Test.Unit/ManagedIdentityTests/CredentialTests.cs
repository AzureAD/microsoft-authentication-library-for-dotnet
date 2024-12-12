// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry.Resources;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{

    [TestClass]
    public class CredentialTests : TestBase
    {
        private const string ImdsEndpoint = "http://169.254.169.254/metadata/identity/oauth2/token";
        internal const string Resource = "https://management.azure.com";

        [TestInitialize]
        public override void TestInitialize()
        {
            // Reset the static cache to ensure tests start fresh
            ManagedIdentityClient.ResetManagedIdentitySourceCache();
        }

        [TestMethod]
        public async Task CredentialSourceFailedFallbackToImdsTestAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ImdsEndpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post);
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post);
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post);
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post);

                httpManager.AddManagedIdentityMockHandler(
                ImdsEndpoint,
                Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                ManagedIdentitySource.Imds);

                var result = await mi.AcquireTokenForManagedIdentity(Resource).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ServiceFabricTests
    {
        private const string Resource = "https://management.azure.com";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public async Task ServiceFabricInvalidEndpointAsync()
        {
            using(new EnvVariableContext())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                SetEnvironmentVariables(ManagedIdentitySource.ServiceFabric, "localhost/token");

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling the shared cache to avoid the test to pass because of the cache
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.ServiceFabric.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.InvalidManagedIdentityEndpoint, ex.ErrorCode);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, "IDENTITY_ENDPOINT", "localhost/token", "Service Fabric"), ex.Message);
            }
        }

        [DataTestMethod]
        [DeploymentItem(@"Resources\testCert.crtfile")]
        [DataRow("invalidThumbprint", SslPolicyErrors.None, true, DisplayName = "ServerCertificateValidationCallback_NoSSLErrors_InvalidThumbprint")]
        [DataRow("E70C50DA4EA66F94229A594BC112CB4B4FF29EB4", SslPolicyErrors.RemoteCertificateNameMismatch, true, DisplayName = "ServerCertificateValidationCallback_SSLErrors_validThumbprint")]
        [DataRow("E70C50DA4EA66F94229A594BC112CB4B4FF29EB4", SslPolicyErrors.None, true, DisplayName = "ServerCertificateValidationCallback_NoSSLErrors_ValidThumbprint")]
        [DataRow("invalidThumbprint", SslPolicyErrors.RemoteCertificateNameMismatch, false, DisplayName = "ServerCertificateValidationCallback_SSLErrors_invalidThumbprint")]
        public void ValidateServerCertificateCallback_ServerCertificateValidationCallback_ReturnsHttpClientHandlerWithCustomValidationCallback(
            string thumbprint, SslPolicyErrors sslPolicyErrors, bool expectedValidationResult)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                SetEnvironmentVariables(ManagedIdentitySource.ServiceFabric, "http://localhost:40342/metadata/identity/oauth2/token", thumbprint: thumbprint);
                var certificate = new X509Certificate2(ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), TestConstants.TestCertPassword);
                var chain = new X509Chain();

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling the shared cache to avoid the test to pass because of the cache
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.BuildConcrete();

                RequestContext requestContext = new RequestContext(mi.ServiceBundle, Guid.NewGuid());

                var sf = ServiceFabricManagedIdentitySource.Create(requestContext);

                Assert.IsInstanceOfType(sf, typeof(ServiceFabricManagedIdentitySource));
                HttpClient httpClient = ((ServiceFabricManagedIdentitySource)sf).GetHttpClientWithSslValidation(requestContext);
                Assert.IsNotNull(httpClient);
                var httpClientHandler = ((ServiceFabricManagedIdentitySource)sf).CreateHandlerWithSslValidation(requestContext.Logger);
                Assert.IsNotNull(httpClientHandler.ServerCertificateCustomValidationCallback);

                var validationResult = httpClientHandler.ServerCertificateCustomValidationCallback(null, certificate, chain, sslPolicyErrors);
                Assert.AreEqual(expectedValidationResult, validationResult);
            }
        }
    }
}

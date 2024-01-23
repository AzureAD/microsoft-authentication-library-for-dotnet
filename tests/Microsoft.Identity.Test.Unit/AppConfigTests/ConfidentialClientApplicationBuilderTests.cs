// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.AppConfigTests
{
    [TestClass]
    [TestCategory(TestCategories.BuilderTests)]
    public class ConfidentialClientApplicationBuilderTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void TestConstructor()
        {
            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                          .WithClientSecret("cats")
                                                          .Build();

            Assert.AreEqual(TestConstants.ClientId, cca.AppConfig.ClientId);
            Assert.IsNotNull(cca.UserTokenCache);

            // Validate Defaults
            Assert.AreEqual(LogLevel.Info, cca.AppConfig.LogLevel);
            Assert.AreEqual(TestConstants.ClientId, cca.AppConfig.ClientId);
            Assert.IsNotNull(cca.AppConfig.ClientName);
            Assert.IsNotNull(cca.AppConfig.ClientVersion);
            Assert.AreEqual(false, cca.AppConfig.EnablePiiLogging);
            Assert.IsNull(cca.AppConfig.HttpClientFactory);
            Assert.AreEqual(false, cca.AppConfig.IsDefaultPlatformLoggingEnabled);
            Assert.IsNull(cca.AppConfig.LoggingCallback);
            Assert.AreEqual(Constants.DefaultConfidentialClientRedirectUri, cca.AppConfig.RedirectUri);
            Assert.AreEqual(null, cca.AppConfig.TenantId);
        }

        private ConfidentialClientApplicationOptions CreateConfidentialClientApplicationOptions()
        {
            return new ConfidentialClientApplicationOptions
            {
                ClientId = TestConstants.ClientId,
                ClientSecret = "the_client_secret",
                TenantId = "the_tenant_id",
            };
        }

        private void TestBuildConfidentialClientFromOptions(ConfidentialClientApplicationOptions options)
        {
            options.ClientSecret = "cats";
            var app = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(options).Build();
            var authorityInfo = ((ConfidentialClientApplication)app).ServiceBundle.Config.Authority.AuthorityInfo;
            Assert.AreEqual(new Uri("https://login.microsoftonline.com/the_tenant_id/"), authorityInfo.CanonicalAuthority);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\testCert.crtfile")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Internal.Analyzers", "IA5352:DoNotMisuseCryptographicApi", Justification = "Suppressing RoslynAnalyzers: Rule: IA5352 - Do Not Misuse Cryptographic APIs in test only code")]
        public void TestBuildWithNoClientSecretButUsingCert()
        {
            var options = new ConfidentialClientApplicationOptions()
            {
                ClientId = TestConstants.ClientId,
                TenantId = "the_tenant_id",
                Instance = "https://login.microsoftonline.com",
            };

            var cert = new X509Certificate2(
               ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), TestConstants.TestCertPassword);

            var app = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(options)
                                                          .WithCertificate(cert)
                                                          .Build();
            var authority = ((ConfidentialClientApplication)app).ServiceBundle.Config.Authority;
            Assert.AreEqual(new Uri("https://login.microsoftonline.com/the_tenant_id/"), authority.AuthorityInfo.CanonicalAuthority);
        }

        [TestMethod]
        public void TestBuildWithInstanceWithTrailingSlash()
        {
            var options = CreateConfidentialClientApplicationOptions();
            options.Instance = "https://login.microsoftonline.com/";
            TestBuildConfidentialClientFromOptions(options);
        }

        [TestMethod]
        public void CacheSynchronization_Default_IsTrue()
        {
            var ccaOptions = new ConfidentialClientApplicationOptions()
            {
                ClientSecret = "secret",
                ClientId = TestConstants.ClientId,
            };
            var cca = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(ccaOptions).Build();
            Assert.IsTrue((cca.AppConfig as ApplicationConfiguration).CacheSynchronizationEnabled);

            cca = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString()).WithClientSecret(TestConstants.ClientSecret).Build();
            Assert.IsTrue((cca.AppConfig as ApplicationConfiguration).CacheSynchronizationEnabled);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CacheSynchronization_WithOptions(bool enableCacheSynchronization)
        {
            var ccaOptions = new ConfidentialClientApplicationOptions
            {
                ClientId = TestConstants.ClientId,
                ClientSecret = "secret",
                EnableCacheSynchronization = enableCacheSynchronization
            };
            var cca = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(ccaOptions).Build();
            Assert.AreEqual(enableCacheSynchronization, (cca.AppConfig as ApplicationConfiguration).CacheSynchronizationEnabled);
        }

        [DataTestMethod]
        [DataRow(false, false, false)]
        [DataRow(true, true, true)]
        [DataRow(true, false, false)]
        [DataRow(false, true, true)]
        public void CacheSynchronization_WithCacheSynchronization_TakesPrecedence(bool optionFlag, bool builderFlag, bool result)
        {
            var options = new ConfidentialClientApplicationOptions
            {
                ClientSecret = "secret",
                ClientId = TestConstants.ClientId,
                EnableCacheSynchronization = optionFlag
            };
            var app = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(options).WithCacheSynchronization(builderFlag).Build();
            Assert.AreEqual(result, (app.AppConfig as ApplicationConfiguration).CacheSynchronizationEnabled);
        }

        [TestMethod]
        public void TestBuildWithInstanceWithoutTrailingSlash()
        {
            var options = CreateConfidentialClientApplicationOptions();
            options.Instance = "https://login.microsoftonline.com";
            TestBuildConfidentialClientFromOptions(options);
        }

        [TestMethod]
        public void TestBuildWithNullInstance()
        {
            var options = CreateConfidentialClientApplicationOptions();
            options.Instance = null;
            TestBuildConfidentialClientFromOptions(options);
        }

        [TestMethod]
        public void TestBuildWithEmptyInstance()
        {
            var options = CreateConfidentialClientApplicationOptions();
            options.Instance = string.Empty;
            TestBuildConfidentialClientFromOptions(options);
        }

        [TestMethod]
        public void TestWithDifferentClientId()
        {
            const string ClientId = "9340c42a-f5de-4a80-aea0-874adc2ca325";
            const string AppIdUri = "https://microsoft.onmicrosoft.com/aa3e634f-58b3-4eb7-b4ed-244c44c29c47";
            var cca = ConfidentialClientApplicationBuilder.Create(ClientId).WithClientSecret("cats").Build();
            Assert.AreEqual(ClientId, cca.AppConfig.ClientId);

            cca = ConfidentialClientApplicationBuilder.Create(AppIdUri).WithClientSecret("cats").Build();
            Assert.AreEqual(AppIdUri, cca.AppConfig.ClientId);
        }

        [TestMethod]
        public void TestConstructor_ClientIdOverride()
        {
            const string ClientId = "73cc145e-798f-430c-8d6d-618f1a5802e9";
            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                .WithClientId(ClientId)
                                                                .WithClientSecret("cats")
                                                                .Build();
            Assert.AreEqual(ClientId, cca.AppConfig.ClientId);
        }

        [TestMethod]
        public void TestConstructor_WithClientNameAndVersion()
        {
            const string ClientName = "my client name";
            const string ClientVersion = "1.2.3.4-prerelease";
            var cca =
                ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                            .WithClientName(ClientName)
                                                            .WithClientVersion(ClientVersion)
                                                            .WithClientSecret("cats")
                                                            .Build();
            Assert.AreEqual(ClientName, cca.AppConfig.ClientName);
            Assert.AreEqual(ClientVersion, cca.AppConfig.ClientVersion);
        }

        [TestMethod]
        public void TestConstructor_WithDebugLoggingCallback()
        {
            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                .WithClientSecret("cats")
                                                                .WithDebugLoggingCallback()
                                                                .Build();
            Assert.IsNotNull(cca.AppConfig.LoggingCallback);
        }

        [TestMethod]
        public void TestConstructor_WithHttpClientFactory()
        {
            var httpClientFactory = NSubstitute.Substitute.For<IMsalHttpClientFactory>();
            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                .WithHttpClientFactory(httpClientFactory)
                                                                .WithClientSecret("cats")
                                                                .Build();
            Assert.AreEqual(httpClientFactory, cca.AppConfig.HttpClientFactory);
        }

        [TestMethod]
        public void TestConstructor_WithMtlsHttpClientFactory()
        {
            var httpClientFactory = NSubstitute.Substitute.For<IMsalMtlsHttpClientFactory>();
            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                .WithHttpClientFactory(httpClientFactory)
                                                                .WithClientSecret("cats")
                                                                .Build();
            Assert.AreEqual(httpClientFactory, cca.AppConfig.HttpClientFactory);
        }

        [TestMethod]
        public void TestConstructor_WithLogging()
        {
            var cca = ConfidentialClientApplicationBuilder
                      .Create(TestConstants.ClientId)
                            .WithClientSecret("cats")
                            .WithLogging((_, _, _) => { }).Build();

            Assert.IsNotNull(cca.AppConfig.LoggingCallback);
        }

        [TestMethod]
        public void TestConstructor_WithRedirectUri()
        {
            const string RedirectUri = "http://some_redirect_uri/";
            var cca = ConfidentialClientApplicationBuilder
                      .Create(TestConstants.ClientId)
                      .WithClientSecret("cats")
                      .WithRedirectUri(RedirectUri).Build();

            Assert.AreEqual(RedirectUri, cca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithNullRedirectUri()
        {
            var cca = ConfidentialClientApplicationBuilder
                      .Create(TestConstants.ClientId)
                      .WithClientSecret("cats")
                      .WithRedirectUri(null).Build();

            Assert.AreEqual(Constants.DefaultConfidentialClientRedirectUri, cca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithEmptyRedirectUri()
        {
            var cca = ConfidentialClientApplicationBuilder
                      .Create(TestConstants.ClientId)
                      .WithClientSecret("cats")
                      .WithRedirectUri(string.Empty).Build();

            Assert.AreEqual(Constants.DefaultConfidentialClientRedirectUri, cca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithWhitespaceRedirectUri()
        {
            var cca = ConfidentialClientApplicationBuilder
                      .Create(TestConstants.ClientId)
                      .WithClientSecret("cats")
                      .WithRedirectUri("      ")
                      .Build();

            Assert.AreEqual(Constants.DefaultConfidentialClientRedirectUri, cca.AppConfig.RedirectUri);
        }

        [TestMethod]
        public void TestConstructor_WithInvalidRedirectUri()
        {
            Assert.ThrowsException<InvalidOperationException>(() =>
                ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                    .WithClientSecret("cats")
                                                    .WithRedirectUri("this is not a valid uri")
                                                    .Build());
        }

        [TestMethod]
        public void TestConstructor_WithTenantId()
        {
            var cca = ConfidentialClientApplicationBuilder
                      .Create(TestConstants.ClientId)
                      .WithClientSecret("cats")
                      .WithTenantId(TestConstants.TenantId)
                      .Build();

            Assert.AreEqual(TestConstants.TenantId, cca.AppConfig.TenantId);
        }

        [TestMethod]
        public void TestConstructor_WithClientSecret()
        {
            const string ClientSecret = "secret value here";
            var cca = ConfidentialClientApplicationBuilder
                      .Create(TestConstants.ClientId)
                      .WithClientSecret(ClientSecret)
                      .Build();

            Assert.IsNotNull(cca.AppConfig.ClientSecret);
            Assert.AreEqual(ClientSecret, cca.AppConfig.ClientSecret);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\testCert.crtfile")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Internal.Analyzers", "IA5352:DoNotMisuseCryptographicApi", Justification = "Suppressing RoslynAnalyzers: Rule: IA5352 - Do Not Misuse Cryptographic APIs in test only code")]
        public void TestConstructor_WithCertificate_X509Certificate2()
        {
            var cert = new X509Certificate2(
                ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), TestConstants.TestCertPassword);

            var cca = ConfidentialClientApplicationBuilder
                      .Create(TestConstants.ClientId)
                      .WithCertificate(cert).Build();

            Assert.IsNotNull(cca.AppConfig.ClientCredentialCertificate);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\valid_cert.cer")]
        public void TestConstructor_WithCertificate_WithoutPrivateKey()
        {
            var cert = new X509Certificate2(
                ResourceHelper.GetTestResourceRelativePath("valid_cert.cer"));

            try
            {
                ConfidentialClientApplicationBuilder
                      .Create(TestConstants.ClientId).WithCertificate(cert).Build();

                Assert.Fail();
            }
            catch (MsalClientException e)
            {
                Assert.IsNotNull(e);
                Assert.AreEqual(MsalError.CertWithoutPrivateKey, e.ErrorCode);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\testCert.crtfile")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Internal.Analyzers", "IA5352:DoNotMisuseCryptographicApi", Justification = "Suppressing RoslynAnalyzers: Rule: IA5352 - Do Not Misuse Cryptographic APIs in test only code")]
        public void TestConstructor_WithCertificate_SendX5C()
        {
            var cert = new X509Certificate2(
                ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), TestConstants.TestCertPassword);

            var app = ConfidentialClientApplicationBuilder
                      .Create(TestConstants.ClientId)
                      .WithCertificate(cert)
                      .Build();

            Assert.IsFalse((app.AppConfig as ApplicationConfiguration).SendX5C);

            app = ConfidentialClientApplicationBuilder
                  .Create(TestConstants.ClientId)
                  .WithCertificate(cert, true)
                  .Build();

            Assert.IsTrue((app.AppConfig as ApplicationConfiguration).SendX5C);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
        public void TestConstructor_WithValidInstanceDicoveryMetadata()
        {
            string instanceMetadataJson = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("CustomInstanceMetadata.json"));
            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                   .WithClientSecret("cats")
                                                   .WithInstanceDiscoveryMetadata(instanceMetadataJson)
                                                   .Build();

            var instanceDiscoveryMetadata = (cca.AppConfig as ApplicationConfiguration).CustomInstanceDiscoveryMetadata;
            Assert.AreEqual(2, instanceDiscoveryMetadata.Metadata.Length);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
        public void TestConstructor_InstanceMetadata_ValidateAuthority_MutuallyExclusive()
        {
            string instanceMetadataJson = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("CustomInstanceMetadata.json"));
            var ex = AssertException.Throws<MsalClientException>(() => ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                  .WithInstanceDiscoveryMetadata(instanceMetadataJson)
                                                  .WithClientSecret("cats")
                                                  .WithAuthority("https://some.authority/bogus/", true)
                                                  .Build());
            Assert.AreEqual(ex.ErrorCode, MsalError.ValidateAuthorityOrCustomMetadata);
        }

        [TestMethod]
        public void TestConstructor_BadInstanceMetadata()
        {
            var ex = AssertException.Throws<MsalClientException>(() => ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                  .WithInstanceDiscoveryMetadata("{bad_json_metadata")
                                                  .WithClientSecret("cats")
                                                  .Build());

            Assert.AreEqual(ex.ErrorCode, MsalError.InvalidUserInstanceMetadata);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        [DataRow(null)] // Not specified, default is true
        public void TestConstructor_WithLegacyCacheCompatibility(bool? isLegacyCacheCompatibilityEnabled)
        {
            var builder = ConfidentialClientApplicationBuilder
                      .Create(TestConstants.ClientId)
                      .WithClientSecret(TestConstants.ClientSecret);

            if (isLegacyCacheCompatibilityEnabled.HasValue)
            {
                builder.WithLegacyCacheCompatibility(isLegacyCacheCompatibilityEnabled.Value);
            }
            else
            {
                isLegacyCacheCompatibilityEnabled = true;
            }

            var cca = builder.Build();

            Assert.AreEqual(isLegacyCacheCompatibilityEnabled, cca.AppConfig.LegacyCacheCompatibilityEnabled);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        [DataRow(null)] // Not specified, default is true
        public void TestConstructor_WithLegacyCacheCompatibility_WithOptions(bool? isLegacyCacheCompatibilityEnabled)
        {
            var options = CreateConfidentialClientApplicationOptions();

            if (isLegacyCacheCompatibilityEnabled.HasValue)
            {
                options.LegacyCacheCompatibilityEnabled = isLegacyCacheCompatibilityEnabled.Value;
            }
            else
            {
                isLegacyCacheCompatibilityEnabled = true;
            }

            var cca = ConfidentialClientApplicationBuilder
                      .CreateWithApplicationOptions(options)
                      .Build();

            Assert.AreEqual(isLegacyCacheCompatibilityEnabled, cca.AppConfig.LegacyCacheCompatibilityEnabled);
        }

        [TestMethod]
        public void WithClientCapabilities()
        {
            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithClientCapabilities(new[] { "cp1", "cp2" })
                .Build();

            CollectionAssert.AreEquivalent(new[] { "cp1", "cp2" }, cca.AppConfig.ClientCapabilities.ToList());
        }

        [TestMethod]
        public void WithClientCapabilitiesViaOptions()
        {
            var options = new ConfidentialClientApplicationOptions
            {
                Instance = "https://login.microsoftonline.com",
                TenantId = "organizations",
                ClientId = TestConstants.ClientId,
                ClientCapabilities = new[] { "cp1", "cp2" }
            };

            var app = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(options)
                .Build();

            CollectionAssert.AreEquivalent(new string[] { "cp1", "cp2" }, app.AppConfig.ClientCapabilities.ToList());
        }

        [TestMethod]
        public async Task Claims_Fail_WhenClaimsIsNotJson_Async()
        {
            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                            .WithClientSecret(TestConstants.ClientSecret)
                            .WithClientCapabilities(TestConstants.ClientCapabilities)
                            .BuildConcrete();

            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithClaims("claims_that_are_not_json")
                    .ExecuteAsync(CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidJsonClaimsFormat, ex.ErrorCode);
        }
    }
}

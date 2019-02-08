using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Azure;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Unit.netcore.AzureTests
{
    [TestClass]
    public class ServicePrincipalTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
        }

        private void AddHostToInstanceCache(IServiceBundle serviceBundle, string host)
        {
            serviceBundle.AadInstanceDiscovery.TryAddValue(
                host,
                new InstanceDiscoveryMetadataEntry
                {
                    PreferredNetwork = host,
                    PreferredCache = host,
                    Aliases = new string[]
                    {
                        host
                    }
                });
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldNotBeAvailableWithoutEnvironmentVarsAsync()
        {
            var probe = new ServicePrincipalProbe(config: new ServicePrincipalConfiguration { });
            Assert.IsFalse(await probe.AvailableAsync().ConfigureAwait(false));
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldThrowIfNotAvailablesAsync()
        {
            var probe = new ServicePrincipalProbe(config: new ServicePrincipalConfiguration { });
            Assert.IsFalse(await probe.AvailableAsync().ConfigureAwait(false));
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await probe.ProviderAsync().ConfigureAwait(false)).ConfigureAwait(false);
            Assert.AreEqual("The required environment variables are not available.", ex.Message);
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldBeAvailableWithServicePrincipalAndSecretAsync()
        {
            var probe = new ServicePrincipalProbe(config: new ServicePrincipalConfiguration
            {
                ClientId = "foo",
                ClientSecret = "bar",
                TenantId = "Bazz"
            });
            Assert.IsTrue(await probe.AvailableAsync().ConfigureAwait(false));
            Assert.IsFalse(probe.IsClientCertificate());
            Assert.IsTrue(probe.IsClientSecret());
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProviderShouldFetchTokenWithServicePrincipalAndSecretAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var authority = "https://login.microsoftonline.com/tenantid/";
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(authority);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                var provider = new ServicePrincipalTokenProvider(authority, "tenantid", MsalTestConstants.ClientId, new ClientCredential(MsalTestConstants.ClientSecret), httpManager);
                var token = await provider.GetTokenAsync(new List<string> { @"https://management.azure.com//.default" }).ConfigureAwait(false);
                Assert.IsNotNull(token);
            }

        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldBeAvailableWithServicePrincipalAndCertificateBase64Async()
        {
            var probe = new ServicePrincipalProbe(config: new ServicePrincipalConfiguration
            {
                ClientId = "foo",
                CertificateBase64 = "bar",
                TenantId = "Bazz"
            });
            Assert.IsTrue(await probe.AvailableAsync().ConfigureAwait(false));
            Assert.IsTrue(probe.IsClientCertificate());
            Assert.IsFalse(probe.IsClientSecret());
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldThrowIfCertificateIsNotInStoreAsync()
        {
            var cfg = new ServicePrincipalConfiguration
            {
                ClientId = "foo",
                CertificateThumbprint = "bar",
                CertificateStoreName = "My",
                TenantId = "Bazz"
            };
            var probe = new ServicePrincipalProbe(config: cfg);
            var msg = $"Unable to find certificate with thumbprint '{cfg.CertificateThumbprint}' in certificate store named 'My' and store location CurrentUser";
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await probe.ProviderAsync().ConfigureAwait(false)).ConfigureAwait(false);
            Assert.AreEqual(msg, ex.Message);
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldBeAvailableWithServicePrincipalAndCertificateThumbAndStoreAsync()
        {
            var probe = new ServicePrincipalProbe(config: new ServicePrincipalConfiguration
            {
                ClientId = "foo",
                CertificateThumbprint = "bar",
                CertificateStoreName = "My",
                TenantId = "Bazz"
            });
            Assert.IsTrue(await probe.AvailableAsync().ConfigureAwait(false));
            Assert.IsTrue(probe.IsClientCertificate());
            Assert.IsFalse(probe.IsClientSecret());
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldNotBeAvailableWithoutTenantIDAsync()
        {
            var probe = new ServicePrincipalProbe(config: new ServicePrincipalConfiguration
            {
                ClientId = "foo",
                CertificateThumbprint = "bar",
                CertificateStoreName = "My",
            });
            Assert.IsFalse(await probe.AvailableAsync().ConfigureAwait(false));
        }
    }

    internal class ServicePrincipalConfiguration : IServicePrincipalConfiguration
    {
        public string ClientId { get; set; }

        public string CertificateBase64 { get; set; }

        public string CertificateThumbprint { get; set; }

        public string CertificateSubjectDistinguishedName { get; set; }

        public string CertificateStoreName { get; set; }

        public string TenantId { get; set; }

        public string ClientSecret { get; set; }

        public string CertificateStoreLocation { get; set; }

        public string Authority => "login.microsoftonline.com";

        public IHttpManager HttpManager { get; set; }
    }
}

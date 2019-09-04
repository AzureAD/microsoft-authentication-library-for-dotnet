// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    [TestClass]
    public class ServicePrincipalTests
    {
        private static readonly Responder DiscoveryResponder = new Responder
        {
            Matcher = (req, state) => req.RequestUri.ToString().StartsWith("https://login.microsoftonline.com/common/discovery/instance", StringComparison.InvariantCulture),
            MockResponse = (req, state) =>
            {
                const string content = @"{
                        ""tenant_discovery_endpoint"":""https://login.microsoftonline.com/tenant/.well-known/openid-configuration"",
                        ""api-version"":""1.1"",
                        ""metadata"":[
                            {
                            ""preferred_network"":""login.microsoftonline.com"",
                            ""preferred_cache"":""login.windows.net"",
                            ""aliases"":[
                                ""login.microsoftonline.com"",
                                ""login.windows.net"",
                                ""login.microsoft.com"",
                                ""sts.windows.net""]},
                            {
                            ""preferred_network"":""login.partner.microsoftonline.cn"",
                            ""preferred_cache"":""login.partner.microsoftonline.cn"",
                            ""aliases"":[
                                ""login.partner.microsoftonline.cn"",
                                ""login.chinacloudapi.cn""]},
                            {
                            ""preferred_network"":""login.microsoftonline.de"",
                            ""preferred_cache"":""login.microsoftonline.de"",
                            ""aliases"":[
                                    ""login.microsoftonline.de""]},
                            {
                            ""preferred_network"":""login.microsoftonline.us"",
                            ""preferred_cache"":""login.microsoftonline.us"",
                            ""aliases"":[
                                ""login.microsoftonline.us"",
                                ""login.usgovcloudapi.net""]},
                            {
                            ""preferred_network"":""login-us.microsoftonline.com"",
                            ""preferred_cache"":""login-us.microsoftonline.com"",
                            ""aliases"":[
                                ""login-us.microsoftonline.com""]}
                        ]
                    }";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new MockJsonContent(content)
                };
            }
        };

        private static readonly Func<string, Responder> TenantDiscoveryResponder = authority =>
        {
            return new Responder
            {
                Matcher = (req, state) => req.RequestUri.ToString() == authority + "v2.0/.well-known/openid-configuration",
                MockResponse = (req, state) =>
                {
                    var qp = "";
                    var authorityUri = new Uri(authority);
                    var path = authorityUri.AbsolutePath.Substring(1);
                    var tenant = path.Substring(0, path.IndexOf("/", StringComparison.Ordinal));
                    if (tenant.ToLowerInvariant().Equals("common", StringComparison.OrdinalIgnoreCase))
                    {
                        tenant = "{tenant}";
                    }

                    if (!string.IsNullOrEmpty(qp))
                    {
                        qp = "?" + qp;
                    }

                    var content = string.Format(CultureInfo.InvariantCulture,
                        "{{\"authorization_endpoint\":\"{0}oauth2/v2.0/authorize{2}\",\"token_endpoint\":\"{0}oauth2/v2.0/token{2}\",\"issuer\":\"https://sts.windows.net/{1}\"}}",
                        authority, tenant, qp);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new MockJsonContent(content)
                    };
                }
            };
        };

        private static readonly Responder ClientCredentialTokenResponder = new Responder
        {
            Matcher = (req, state) => req.RequestUri.ToString().EndsWith("oauth2/v2.0/token") && req.Method == HttpMethod.Post,
            MockResponse = (req, state) =>
            {
                const string token = "superdupertoken";
                const string tokenContent = "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"" + token + "\"}";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new MockJsonContent(tokenContent)
                };
            }
        };

        private IConfiguration FakeConfiguration(IEnumerable<KeyValuePair<string,string>> initialData = null)
        {
            initialData = initialData ?? new List<KeyValuePair<string, string>>();
            return new ConfigurationBuilder().AddInMemoryCollection(initialData).Build();
        }

        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldNotBeAvailableWithoutEnvironmentVarsAsync()
        {

            var provider = new ServicePrincipalTokenProvider(config: FakeConfiguration());
            Assert.IsFalse(await provider.IsAvailableAsync().ConfigureAwait(false));
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldThrowIfNotAvailableAsync()
        {
            var provider = new ServicePrincipalTokenProvider(config: FakeConfiguration());
            Assert.IsFalse(await provider.IsAvailableAsync().ConfigureAwait(false));
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await provider.GetTokenAsync(new List<string>{"foo"})
                .ConfigureAwait(false)).ConfigureAwait(false);
            Assert.AreEqual("The required environment variables are not available.", ex.Message);
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldBeAvailableWithServicePrincipalAndSecretAsync()
        {
            var config = FakeConfiguration(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(Constants.AzureClientIdEnvName, "foo"),
                new KeyValuePair<string, string>(Constants.AzureClientSecretEnvName, "bar"),
                new KeyValuePair<string, string>(Constants.AzureTenantIdEnvName, "Bazz")
            });
            var provider = new ServicePrincipalTokenProvider(config: config);
            Assert.IsTrue(await provider.IsAvailableAsync().ConfigureAwait(false));
            Assert.IsFalse(provider.IsClientCertificate());
            Assert.IsTrue(provider.IsClientSecret());
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProviderShouldFetchTokenWithServicePrincipalAndSecretAsync()
        {
            const string authority = "https://login.microsoftonline.com/tenantid/";
            var handler = new MockManagedIdentityHttpMessageHandler();
            handler.Responders.Add(DiscoveryResponder);
            handler.Responders.Add(TenantDiscoveryResponder(authority));
            handler.Responders.Add(ClientCredentialTokenResponder);
            var clientFactory = new ClientFactory(new HttpClient(handler));
            var clientId = Guid.NewGuid();
            var provider = new InternalServicePrincipalTokenProvider(authority, "tenantid", clientId.ToString(), "someSecret", clientFactory);
            var scopes = new List<string> {@"https://management.azure.com//.default"};
            var token = await provider.GetTokenAsync(scopes, CancellationToken.None)
                .ConfigureAwait(false);
            Assert.IsNotNull(token);
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldBeAvailableWithServicePrincipalAndCertificateBase64Async()
        {
            var config = FakeConfiguration(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(Constants.AzureClientIdEnvName, "foo"),
                new KeyValuePair<string, string>(Constants.AzureCertificateEnvName, "bar"),
                new KeyValuePair<string, string>(Constants.AzureTenantIdEnvName, "Bazz")
            });
            var provider = new ServicePrincipalTokenProvider(config: config);
            Assert.IsTrue(await provider.IsAvailableAsync().ConfigureAwait(false));
            Assert.IsTrue(provider.IsClientCertificate());
            Assert.IsFalse(provider.IsClientSecret());
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldThrowIfCertificateIsNotInStoreAsync()
        {
            var config = FakeConfiguration(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(Constants.AzureClientIdEnvName, "foo"),
                new KeyValuePair<string, string>(Constants.AzureCertificateThumbprintEnvName, "bar"),
                new KeyValuePair<string, string>(Constants.AzureCertificateStoreEnvName, "My"),
                new KeyValuePair<string, string>(Constants.AzureTenantIdEnvName, "Bazz")
            });
            var provider = new ServicePrincipalTokenProvider(config: config);
            var msg = "Unable to find certificate with thumbprint 'bar' in certificate store named 'My' and store location CurrentUser";
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await provider.GetTokenAsync(new List<string>{"foo"}).ConfigureAwait(false)).ConfigureAwait(false);
            Assert.AreEqual(msg, ex.Message);
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldThrowIfCertificateIsNotInStoreDistinguishedNameAsync()
        {
            var config = FakeConfiguration(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(Constants.AzureClientIdEnvName, "foo"),
                new KeyValuePair<string, string>(Constants.AzureCertificateSubjectDistinguishedNameEnvName, "bar"),
                new KeyValuePair<string, string>(Constants.AzureCertificateStoreEnvName, "My"),
                new KeyValuePair<string, string>(Constants.AzureTenantIdEnvName, "Bazz")
            });
            var provider = new ServicePrincipalTokenProvider(config: config);
            var msg = "Unable to find certificate with distinguished name 'bar' in certificate store named 'My' and store location CurrentUser";
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await provider.GetTokenAsync(new List<string>{"foo"}).ConfigureAwait(false)).ConfigureAwait(false);
            Assert.AreEqual(msg, ex.Message);
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldBeAvailableWithServicePrincipalAndCertificateThumbAndStoreAsync()
        {
            var config = FakeConfiguration(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(Constants.AzureClientIdEnvName, "foo"),
                new KeyValuePair<string, string>(Constants.AzureCertificateThumbprintEnvName, "bar"),
                new KeyValuePair<string, string>(Constants.AzureCertificateStoreEnvName, "My"),
                new KeyValuePair<string, string>(Constants.AzureTenantIdEnvName, "Bazz")
            });
            var provider = new ServicePrincipalTokenProvider(config: config);
            Assert.IsTrue(await provider.IsAvailableAsync().ConfigureAwait(false));
            Assert.IsTrue(provider.IsClientCertificate());
            Assert.IsFalse(provider.IsClientSecret());
        }

        [TestMethod]
        [TestCategory("ServicePrincipalTests")]
        public async Task ProbeShouldNotBeAvailableWithoutTenantIdAsync()
        {
            var config = FakeConfiguration(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(Constants.AzureClientIdEnvName, "foo"),
                new KeyValuePair<string, string>(Constants.AzureCertificateThumbprintEnvName, "bar"),
                new KeyValuePair<string, string>(Constants.AzureCertificateStoreEnvName, "My")
            });
            var provider = new ServicePrincipalTokenProvider(config: config);
            Assert.IsFalse(await provider.IsAvailableAsync().ConfigureAwait(false));
        }
    }

    internal class ClientFactory : IMsalHttpClientFactory
    {
        private readonly HttpClient _client;
        public ClientFactory(HttpClient client) => _client = client;
        public HttpClient GetHttpClient() => _client;
    }
}

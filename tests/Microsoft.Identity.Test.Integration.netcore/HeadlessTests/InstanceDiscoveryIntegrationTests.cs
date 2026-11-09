// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class InstanceDiscoveryIntegrationTests
    {
        private static readonly string[] s_scopes = { "User.Read" };

        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task AuthorityMigrationAsync()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppPCAClient).ConfigureAwait(false);

            IPublicClientApplication pca = PublicClientApplicationBuilder
                .Create(app.AppId)
                .WithAuthority("https://login.windows.net/" + user.TenantId + "/")
                .WithTestLogging()
                .Build();

            Trace.WriteLine("Acquire a token using a not so common authority alias");

            #pragma warning disable CS0618 // Type or member is obsolete
            AuthenticationResult authResult = await pca.AcquireTokenByUsernamePassword(
               s_scopes,
                user.Upn,
                user.GetOrFetchPassword())
                // BugBug https://identitydivision.visualstudio.com/Engineering/_workitems/edit/776308/
                // sts.windows.net fails when doing instance discovery, e.g.:
                // https://sts.windows.net/common/discovery/instance?api-version=1.1&authorization_endpoint=https%3A%2F%2Fsts.windows.net%2Ff645ad92-e38d-4d1a-b510-d1b09a74a8ca%2Foauth2%2Fv2.0%2Fauthorize
                .WithTenantId(user.TenantId)
                .ExecuteAsync()
                .ConfigureAwait(false);
            #pragma warning restore CS0618

            Assert.IsNotNull(authResult.AccessToken);

            Trace.WriteLine("Acquire a token silently using the common authority alias");

            authResult = await pca.AcquireTokenSilent(s_scopes, (await pca.GetAccountsAsync().ConfigureAwait(false)).First())
                .WithTenantId("common")
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult.AccessToken);
        }

        [TestMethod]
        public async Task FailedAuthorityValidationTestAsync()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppPCAClient).ConfigureAwait(false);

            IPublicClientApplication pca = PublicClientApplicationBuilder
                .Create(app.AppId)
                .WithAuthority("https://bogus.microsoft.com/common")
                .WithTestLogging()
                .Build();

            Trace.WriteLine("Acquire a token using a not so common authority alias");

            #pragma warning disable CS0618 // Type or member is obsolete
            MsalServiceException exception = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                 pca.AcquireTokenByUsernamePassword(
                    s_scopes,
                     user.Upn,
                     user.GetOrFetchPassword())
                     .ExecuteAsync())
                .ConfigureAwait(false);
            #pragma warning restore CS0618

            Assert.Contains("AADSTS50049", exception.Message);
            Assert.AreEqual("invalid_instance", exception.ErrorCode);
        }

        [TestMethod]
        public async Task AuthorityValidationTestWithFalseValidateAuthorityAsync()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppPCAClient).ConfigureAwait(false);

            IPublicClientApplication pca = PublicClientApplicationBuilder
                .Create(app.AppId)
                .WithAuthority("https://bogus.microsoft.com/common", false)
                .WithTestLogging()
                .Build();

            Trace.WriteLine("Acquire a token using a not so common authority alias");

            #pragma warning disable CS0618 // Type or member is obsolete
            _ = await AssertException.TaskThrowsAsync<HttpRequestException>(() =>
                 pca.AcquireTokenByUsernamePassword(
                    s_scopes,
                     user.Upn,
                     user.GetOrFetchPassword())
                     .ExecuteAsync())
                .ConfigureAwait(false);
            #pragma warning restore CS0618
        }

        /// <summary>
        /// If this test fails, please update the <see cref="KnownMetadataProvider"/> to
        /// use whatever Evo uses (i.e. the aliases, preferred network / metadata from the url below).
        /// Uses the TSE tenant authority for live discovery. PPE metadata is verified by offline unit tests.
        /// </summary>
        [TestMethod]
        public async Task KnownInstanceMetadataIsUpToDateAsync()
        {
            const string tseTenantId = "d8d14ca6-f1fa-41c1-ae95-0db6258ec0a0";
            string authorizationEndpoint = $"https://login.microsoftonline.com/{tseTenantId}/oauth2/v2.0/authorize";
            string validDiscoveryUri = $"https://login.microsoftonline.com/common/discovery/instance?api-version=1.1&authorization_endpoint={Uri.EscapeDataString(authorizationEndpoint)}";
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage discoveryResponse = await httpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Get,
                    validDiscoveryUri)).ConfigureAwait(false);
            string discoveryJson = await discoveryResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!discoveryResponse.IsSuccessStatusCode)
            {
                Assert.Fail(
                    $"TSE authority discovery returned {(int)discoveryResponse.StatusCode} " +
                    $"{discoveryResponse.ReasonPhrase}. Body: {Truncate(discoveryJson)}");
            }

            InstanceDiscoveryMetadataEntry[] actualMetadata = JsonHelper.DeserializeFromJson<InstanceDiscoveryResponse>(discoveryJson).Metadata;

            var processedMetadata = new Dictionary<string, InstanceDiscoveryMetadataEntry>();
            foreach (InstanceDiscoveryMetadataEntry entry in actualMetadata)
            {
                foreach (var alias in entry.Aliases)
                {
                    processedMetadata[alias] = entry;
                }
            }

            IDictionary<string, InstanceDiscoveryMetadataEntry> allKnownMetadata = 
                KnownMetadataProvider.GetAllEntriesForTest();

            // These clouds are not returned for the TSE production authority; PPE remains covered offline.
            var cloudsNotInDiscovery = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "login.windows-ppe.net",
                "login.sovcloud-identity.fr",  // Bleu (France)
                "login.sovcloud-identity.de",  // Delos (Germany)
                "login.sovcloud-identity.sg"   // GovSG (Singapore)
            };

            IDictionary<string, InstanceDiscoveryMetadataEntry> expectedMetadata = 
                allKnownMetadata.Where(kvp => !cloudsNotInDiscovery.Contains(kvp.Value.PreferredNetwork))
                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            CoreAssert.AssertDictionariesAreEqual(
                expectedMetadata,
                processedMetadata,
                new InstanceDiscoveryMetadataEntryComparer());
        }

        private static string Truncate(string s, int maxLength = 500)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            return s.Length <= maxLength ? s : s.Substring(0, maxLength);
        }

        private class InstanceDiscoveryMetadataEntryComparer : IEqualityComparer<InstanceDiscoveryMetadataEntry>
        {
            public bool Equals(InstanceDiscoveryMetadataEntry x, InstanceDiscoveryMetadataEntry y)
            {
                return x != null &&
                       y != null &&
                       string.Equals(x.PreferredCache, y.PreferredCache, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(x.PreferredNetwork, y.PreferredNetwork, StringComparison.OrdinalIgnoreCase) &&
                       Enumerable.SequenceEqual(x.Aliases, y.Aliases, StringComparer.OrdinalIgnoreCase);
            }

            public int GetHashCode(InstanceDiscoveryMetadataEntry obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
